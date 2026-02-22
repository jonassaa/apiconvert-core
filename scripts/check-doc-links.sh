#!/usr/bin/env bash
set -euo pipefail

root_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "${root_dir}"

declare -a markdown_files=(
  "src/Apiconvert.Core/README.md"
  "src/apiconvert-core/README.md"
)

declare -a required_links=(
  "../../docs/diagnostics/error-codes.md"
)

for markdown_file in "${markdown_files[@]}"; do
  for required_link in "${required_links[@]}"; do
    if ! grep -Fq "${required_link}" "${markdown_file}"; then
      echo "Missing required link '${required_link}' in ${markdown_file}"
      exit 1
    fi

    markdown_dir="$(dirname "${markdown_file}")"
    resolved_path="${markdown_dir}/${required_link}"
    if [[ ! -f "${resolved_path}" ]]; then
      echo "Broken local link '${required_link}' in ${markdown_file} (resolved: ${resolved_path})"
      exit 1
    fi
  done
done

echo "Documentation links validated."
