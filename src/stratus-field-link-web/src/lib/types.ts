export type SketchStatus = 'Draft' | 'Published' | 'UnderReview' | 'Approved' | 'Rejected';

export interface SketchSummary {
  id: string;
  packageId: string;
  crewId: string;
  discipline: string;
  diameterInches: number;
  material: string;
  nodeCount: number;
  lengthFeet: number;
  clientLengthFeet: number;
  elevationChangeFeet: number;
  lengthMismatch: boolean;
  status: SketchStatus;
  capturedAtUtc: string;
  publishedAtUtc: string | null;
  clientVersion: string;
}

export interface StationCount {
  key: string;
  count: number;
}

export interface Dashboard {
  totalSketches: number;
  countsByStatus: StationCount[];
  countsByCrew: StationCount[];
  needsReviewCount: number;
  staleReviewCount: number;
  lengthMismatchCount: number;
  totalPublishedFeet: number;
  oldestAwaitingReview: SketchSummary[];
}
