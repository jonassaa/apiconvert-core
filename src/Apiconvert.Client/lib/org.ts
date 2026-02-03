export type OrgSummary = { id: string; name: string; slug: string };

export function normalizeOrgList(
  relation:
    | OrgSummary
    | OrgSummary[]
    | null
    | undefined
): OrgSummary[] {
  if (!relation) return [];
  return Array.isArray(relation) ? relation : [relation];
}

export function isAdminRole(role?: string | null) {
  return role === "owner" || role === "admin";
}

export function canChangeMemberRole({
  currentRole,
  nextRole,
  ownerCount,
}: {
  currentRole: string;
  nextRole: string;
  ownerCount: number;
}) {
  if (currentRole === "owner" && nextRole !== "owner" && ownerCount <= 1) {
    return false;
  }
  return true;
}

export function canRemoveMember({
  currentRole,
  ownerCount,
}: {
  currentRole: string;
  ownerCount: number;
}) {
  if (currentRole === "owner" && ownerCount <= 1) {
    return false;
  }
  return true;
}
