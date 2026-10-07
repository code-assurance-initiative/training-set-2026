import type { DocumentRecord } from '../../src/documents/document-repository.js';
import type { ReportDefinition, ReportRow } from '../../src/reports/report-model.js';

export const documentId = '2f1c0c5e-8a43-4d0e-9a51-0b8f5e6c7d21';
export const reportId = '5b6a3d1e-2c4f-4a8b-9e7d-1f0a2b3c4d5e';

export const sampleDocument: DocumentRecord = {
  id: documentId,
  number: 'HR-2024-001337',
  title: 'Staff handbook <revised>',
  collection: 'HR',
  pageCount: 42,
  createdAt: new Date('2024-03-01T09:30:00.000Z'),
  storagePath: 'hr/2024/handbook.docx',
};

export const sampleReport: ReportDefinition = {
  id: reportId,
  name: 'HR intake',
  owner: 'archivist-4',
  collection: 'HR',
  updatedAt: new Date('2026-09-30T12:00:00.000Z'),
};

export const sampleRows: ReportRow[] = [
  {
    number: 'HR-2024-000002',
    title: 'Pay scales',
    collection: 'HR',
    pageCount: 3,
    createdAt: new Date('2024-01-02T00:00:00.000Z'),
  },
  {
    number: 'HR-2024-000001',
    title: 'Onboarding',
    collection: 'HR',
    pageCount: null,
    createdAt: new Date('2024-01-01T00:00:00.000Z'),
  },
  {
    number: 'HR-2024-000003',
    title: 'Leave policy',
    collection: 'HR',
    pageCount: 11,
    createdAt: new Date('2024-01-03T00:00:00.000Z'),
  },
];
