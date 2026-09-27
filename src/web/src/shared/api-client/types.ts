/**
 * Typed shapes for the planned `/api/v1/...` endpoints.
 *
 * These mirror the data model in
 * docs/research/rapor-m365-operasyon-zekasi-platform-plani.md §3
 * (Evidence, WorkItem, Project/PhaseTransition, ActionProposal, Event, ...).
 *
 * TODO(Faz 1, S1): once OpsIntel.Contracts + the OpenAPI spec exist
 * (docs/api/openapi.json), regenerate this file from the spec instead of
 * hand-maintaining it. Keep the shapes here in sync with §3 until then.
 */

export type WorkItemKind =
  | 'task'
  | 'commitment'
  | 'request'
  | 'follow_up'
  | 'decision'
  | 'risk'
  | 'assumption'
  | 'issue'
  | 'dependency'
  | 'open_question'
  | 'obligation';

export type ReviewState = 'suggested' | 'accepted' | 'edited' | 'rejected';

export type ConfidenceLevel = 'high' | 'medium' | 'low';

export interface Evidence {
  id: string;
  sourceType: 'mail' | 'attachment' | 'sharepoint_file' | 'onedrive_file' | 'teams_transcript' | 'calendar_event';
  exactQuote: string;
  author: string;
  sourceTimestampUtc: string;
  outlookWebLink?: string;
  verified: boolean;
}

export interface WorkItem {
  id: string;
  kind: WorkItemKind;
  title: string;
  description?: string;
  projectId: string;
  owner?: string;
  counterparty?: string;
  dueAtUtc?: string;
  dueText?: string;
  status: string;
  confidence: ConfidenceLevel;
  reviewState: ReviewState;
  evidence: Evidence[];
}

export type ProjectPhase =
  | 'lead_proposal'
  | 'negotiation_contract'
  | 'kickoff_initiation'
  | 'design'
  | 'execution'
  | 'test_uat'
  | 'delivery_go_live'
  | 'invoicing_payment'
  | 'warranty_support'
  | 'closure';

export interface Project {
  id: string;
  name: string;
  customerOrg: string;
  currentPhase: ProjectPhase;
  health: {
    score: number; // 0-100
    band: 'good' | 'watch' | 'at_risk';
    drivers: string[];
  };
}

export interface PhaseTransition {
  id: string;
  projectId: string;
  fromPhase: ProjectPhase | null;
  toPhase: ProjectPhase;
  atUtc: string;
  confirmed: boolean;
  evidenceIds: string[];
}

export type ActionProposalKind = 'reply_draft' | 'task' | 'calendar_hold' | 'nudge' | 'status_report';

export type ActionProposalStatus =
  | 'suggested'
  | 'pending_approval'
  | 'approved'
  | 'rejected'
  | 'edited'
  | 'executing'
  | 'done'
  | 'failed';

export interface ActionProposal {
  id: string;
  kind: ActionProposalKind;
  title: string;
  summary: string;
  payloadPreview: string;
  rationale: string;
  evidence: Evidence[];
  riskFlags: string[];
  status: ActionProposalStatus;
  projectId?: string;
}

export interface TimelineEvent {
  id: string;
  projectId: string;
  activity: string;
  timestampUtc: string;
  actor: string;
  evidence?: Evidence;
}

export interface MyWorkSummary {
  myTasks: WorkItem[];
  waitingOn: WorkItem[];
}
