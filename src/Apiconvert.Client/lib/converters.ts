export function normalizeConverterNameForUrl(name: string) {
  return encodeURIComponent(name.trim().toLowerCase());
}

export function normalizeConverterNameParam(param: string) {
  try {
    return decodeURIComponent(param).trim().toLowerCase();
  } catch {
    return param.trim().toLowerCase();
  }
}
