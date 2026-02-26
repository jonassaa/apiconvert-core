#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/.." && pwd)"

dotnet_results_root="${repo_root}/tests/nuget/Apiconvert.Core.Tests/TestResults/Coverage"
dotnet_report_dir="${dotnet_results_root}/report"
npm_results_root="${repo_root}/tests/npm/apiconvert-core-tests/coverage"

rm -rf "${dotnet_results_root}" "${npm_results_root}"

pushd "${repo_root}" >/dev/null

echo "Running .NET coverage..."
dotnet test tests/nuget/Apiconvert.Core.Tests/Apiconvert.Core.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory "${dotnet_results_root}" \
  --configuration Debug

dotnet tool restore

dotnet tool run reportgenerator \
  -reports:"${dotnet_results_root}/**/coverage.cobertura.xml" \
  -targetdir:"${dotnet_report_dir}" \
  -reporttypes:"Html;TextSummary"

dotnet_summary_file="${dotnet_report_dir}/Summary.txt"
if [[ -f "${dotnet_summary_file}" ]]; then
  echo
  echo ".NET coverage summary:"
  cat "${dotnet_summary_file}"
fi

echo
echo "Running TypeScript coverage..."
npm --prefix tests/npm/apiconvert-core-tests run coverage

echo
echo ".NET HTML report: ${dotnet_report_dir}/index.html"
echo "TypeScript HTML report: ${npm_results_root}/index.html"

popd >/dev/null
