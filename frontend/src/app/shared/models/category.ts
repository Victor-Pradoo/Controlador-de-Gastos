export type CategoryScope = 'Variable' | 'Fixed' | 'Income';

export interface CategoryDefinition {
  readonly name: string;
  readonly color: string;
  readonly scope: CategoryScope;
}
