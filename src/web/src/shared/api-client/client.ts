import type { ActionProposal, MyWorkSummary, PhaseTransition, Project, TimelineEvent, WorkItem } from './types';
import {
  mockActionProposals,
  mockMyWork,
  mockPhaseTransitions,
  mockProjects,
  mockTimelineEvents,
  mockWorkItems,
} from './mockData';

/**
 * Placeholder typed fetch client for `/api/v1/...`.
 *
 * TODO(Faz 1): this will be generated from docs/api/openapi.json once
 * OpsIntel.Host exposes the real REST + SSE API (see §5, "src/api/" in the
 * repo tree). Until then every method here returns canned, in-memory mock
 * data shaped exactly like the planned response DTOs, so feature code can be
 * written against the final contract without a running backend.
 *
 * When the generated client lands, only this file (and mockData.ts, which
 * can move to test fixtures) should need to change — feature code imports
 * from `shared/api-client` and is otherwise unaffected.
 */

const SIMULATED_LATENCY_MS = 120;

function delay<T>(value: T): Promise<T> {
  return new Promise((resolve) => setTimeout(() => resolve(value), SIMULATED_LATENCY_MS));
}

export const apiClient = {
  projects: {
    list(): Promise<Project[]> {
      return delay(mockProjects);
    },
    get(id: string): Promise<Project | undefined> {
      return delay(mockProjects.find((p) => p.id === id));
    },
    workItems(projectId: string): Promise<WorkItem[]> {
      return delay(mockWorkItems.filter((w) => w.projectId === projectId));
    },
    phaseTransitions(projectId: string): Promise<PhaseTransition[]> {
      return delay(mockPhaseTransitions.filter((p) => p.projectId === projectId));
    },
  },
  myWork: {
    summary(): Promise<MyWorkSummary> {
      return delay(mockMyWork);
    },
  },
  reviewQueue: {
    list(): Promise<ActionProposal[]> {
      return delay(mockActionProposals);
    },
    // TODO(Faz 1, S7): wire to POST /api/v1/action-proposals/{id}/approve|reject|edit
    approve(_id: string): Promise<void> {
      return delay(undefined);
    },
    reject(_id: string, _reasonCode: string): Promise<void> {
      return delay(undefined);
    },
    edit(_id: string, _payload: string): Promise<void> {
      return delay(undefined);
    },
  },
  timeline: {
    list(projectId?: string): Promise<TimelineEvent[]> {
      const events = projectId ? mockTimelineEvents.filter((e) => e.projectId === projectId) : mockTimelineEvents;
      return delay(events);
    },
  },
};
