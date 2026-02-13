#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/.." && pwd)"
schemas_root="${repo_root}/schemas/rules"

if [[ ! -d "${schemas_root}" ]]; then
  echo "schemas/rules directory is missing."
  exit 1
fi

version_dirs=()
for dir in "${schemas_root}"/v*; do
  [[ -d "${dir}" ]] || continue
  version_dirs+=("$(basename "${dir}")")
done

if [[ ${#version_dirs[@]} -gt 0 ]]; then
  sorted_version_dirs=()
  while IFS= read -r line; do
    sorted_version_dirs+=("${line}")
  done < <(printf '%s\n' "${version_dirs[@]}" | sort -V)
  version_dirs=("${sorted_version_dirs[@]}")
fi

if [[ ${#version_dirs[@]} -eq 0 ]]; then
  echo "No versioned schema directories found under schemas/rules."
  exit 1
fi

for version_dir in "${version_dirs[@]}"; do
  if [[ ! "${version_dir}" =~ ^v[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    echo "Invalid schema version directory format: ${version_dir}"
    exit 1
  fi

  if [[ ! -f "${schemas_root}/${version_dir}/schema.json" ]]; then
    echo "Missing schema.json for ${version_dir}"
    exit 1
  fi
done

latest_version_dir="${version_dirs[${#version_dirs[@]}-1]}"
latest_schema="${schemas_root}/${latest_version_dir}/schema.json"
current_schema="${schemas_root}/current/schema.json"

if [[ ! -f "${current_schema}" ]]; then
  echo "Missing current/schema.json."
  exit 1
fi

if ! cmp -s "${latest_schema}" "${current_schema}"; then
  echo "schemas/rules/current/schema.json must exactly match ${latest_version_dir}/schema.json."
  exit 1
fi

diff_base="${SCHEMA_CHECK_BASE:-}"
if [[ -z "${diff_base}" ]]; then
  if [[ -n "${GITHUB_BASE_REF:-}" ]]; then
    diff_base="origin/${GITHUB_BASE_REF}"
  elif git rev-parse --verify HEAD^ >/dev/null 2>&1; then
    diff_base="HEAD^"
  fi
fi

if [[ -n "${diff_base}" ]]; then
  if ! git rev-parse --verify "${diff_base}" >/dev/null 2>&1; then
    echo "SCHEMA_CHECK_BASE '${diff_base}' does not resolve in this checkout."
    exit 1
  fi

  while IFS=$'\t' read -r status path _; do
    [[ -z "${status}" || -z "${path}" ]] && continue
    [[ "${path}" =~ ^schemas/rules/v[0-9]+\.[0-9]+\.[0-9]+/schema\.json$ ]] || continue
    if [[ "${status}" != A* ]]; then
      echo "Versioned schema files are immutable after release: blocked change ${status} ${path}"
      exit 1
    fi
  done < <(git diff --name-status "${diff_base}...HEAD" -- 'schemas/rules/v*/schema.json')
fi

release_tag="${RELEASE_TAG:-}"
if [[ -z "${release_tag}" && "${GITHUB_REF_TYPE:-}" == "tag" ]]; then
  release_tag="${GITHUB_REF_NAME:-}"
fi

if [[ -n "${release_tag}" ]]; then
  if [[ ! "${release_tag}" =~ ^v[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    echo "Release tag '${release_tag}' is not in v<major>.<minor>.<patch> format."
    exit 1
  fi

  release_schema="${schemas_root}/${release_tag}/schema.json"
  if [[ ! -f "${release_schema}" ]]; then
    echo "Missing release schema for tag ${release_tag}: ${release_schema}"
    exit 1
  fi
fi

echo "Schema versioning policy check passed."
