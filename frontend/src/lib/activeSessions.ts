import type { ReleasedSessionTable, Session, SessionActiveTable } from "@/types";

export function getSessionActiveAssignments(session: Session): SessionActiveTable[] {
  if (session.activeAssignments?.length) return session.activeAssignments;
  return session.currentTable ? [{ ...session.currentTable, assignmentId: Number(session.currentTable.assignmentId || 0) }] : [];
}

export function getSessionReleasedAssignments(session: Session): ReleasedSessionTable[] {
  return session.releasedAssignments || [];
}

function mergeByAssignmentId<T extends { assignmentId?: number; tableId?: number }>(current: T[], next: T[]): T[] {
  const map = new Map<string, T>();
  for (const item of [...current, ...next]) {
    const key = item.assignmentId ? `assignment-${item.assignmentId}` : `table-${item.tableId || ""}`;
    map.set(key, item);
  }
  return [...map.values()];
}

export function normalizeActiveSessions(items: Session[]): Session[] {
  const map = new Map<number, Session>();

  for (const item of items) {
    const sessionId = Number(item.sessionId);
    const activeAssignments = getSessionActiveAssignments(item);
    const releasedAssignments = getSessionReleasedAssignments(item);
    const existing = map.get(sessionId);

    if (!existing) {
      map.set(sessionId, {
        ...item,
        activeAssignments,
        releasedAssignments,
        activeTableCount: item.activeTableCount ?? activeAssignments.length,
        releasedTableCount: item.releasedTableCount ?? releasedAssignments.length,
        currentTable: item.currentTable || activeAssignments[0]
      });
      continue;
    }

    const mergedActive = mergeByAssignmentId(existing.activeAssignments || [], activeAssignments);
    const mergedReleased = mergeByAssignmentId(existing.releasedAssignments || [], releasedAssignments);
    map.set(sessionId, {
      ...existing,
      ...item,
      activeAssignments: mergedActive,
      releasedAssignments: mergedReleased,
      activeTableCount: mergedActive.length,
      releasedTableCount: mergedReleased.length,
      currentTable: existing.currentTable || item.currentTable || mergedActive[0]
    });
  }

  return [...map.values()];
}
