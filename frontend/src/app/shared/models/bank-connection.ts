export type BankConnectionStatus = 'Pending' | 'Active' | 'RequiresAction' | 'Error' | 'Disabled';

export interface BankConnection {
  readonly id: string;
  readonly provider: string;
  readonly institutionName: string;
  readonly status: BankConnectionStatus;
  readonly lastSyncedAt: string | null;
}

export interface BankSyncResult {
  readonly imported: number;
  readonly skipped: number;
  readonly failed: number;
  readonly syncedAt: string;
}
