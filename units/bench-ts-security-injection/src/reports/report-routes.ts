import express, { Router, type RequestHandler, type Response } from 'express';
import { z } from 'zod';
import { sendProblem } from '../http/problem.js';
import { answerTextBody } from '../http/text-body.js';
import { requireScope, Scopes } from '../http/scopes.js';
import { parseInput } from '../http/validation.js';
import { InvalidColumnMappingError, parseColumnMapping } from './column-mapping.js';
import { FormulaError } from './formula-evaluator.js';
import { InvalidLayoutError, parseLayout } from './layout-format.js';
import { reportColumns, type ReportDefinition } from './report-model.js';
import type { ReportRepository } from './report-repository.js';
import type { ReportService } from './report-service.js';
import { scheduleStates, type ScheduleRepository } from './schedule-repository.js';

const ownerQuery = z.object({ owner: z.string().min(1).max(100) });

const scheduleQuery = z.object({
  state: z.enum(scheduleStates).default('all'),
  timezone: z.string().min(1).max(64).optional(),
});

const previewBody = z.strictObject({
  computed: z
    .array(z.strictObject({ name: z.string().min(1).max(60), formula: z.string().min(1).max(500) }))
    .max(10)
    .default([]),
  sort: z
    .strictObject({ column: z.enum(reportColumns), direction: z.enum(['asc', 'desc']) })
    .optional(),
  settings: z.record(z.string(), z.unknown()).optional(),
});

const yamlBody = express.text({ type: ['application/yaml', 'text/yaml'], limit: '64kb' });

export interface ReportRouteDependencies {
  readonly reports: Pick<ReportService, 'get' | 'preview' | 'render' | 'remap'>;
  readonly summaries: Pick<ReportRepository, 'listByOwner'>;
  readonly schedules: Pick<ScheduleRepository, 'forReport'>;
}

const isYamlRefusal = (error: unknown): error is Error =>
  error instanceof InvalidLayoutError || error instanceof InvalidColumnMappingError;

type RenderReport = (report: ReportDefinition, text: string) => Promise<object>;

export function reportRoutes({ reports, summaries, schedules }: ReportRouteDependencies) {
  const read = requireScope(Scopes.reportsRead);
  const write = requireScope(Scopes.reportsWrite);

  /** The report named by the route, or undefined after answering 400 or 404. */
  async function findReport(id: unknown, res: Response): Promise<ReportDefinition | undefined> {
    const reportId = parseInput(z.uuid(), id, res);
    const report = reportId === undefined ? undefined : await reports.get(reportId);
    if (reportId !== undefined && !report) {
      sendProblem(res, 404, 'No report has this id.');
    }
    return report;
  }

  const listReports: RequestHandler = async (req, res) => {
    const query = parseInput(ownerQuery, req.query, res);
    if (query) {
      res.json({ reports: await summaries.listByOwner(query.owner) });
    }
  };

  const listSchedules: RequestHandler = async (req, res) => {
    const report = await findReport(req.params.id, res);
    const query = report && parseInput(scheduleQuery, req.query, res);
    if (report && query) {
      res.json({ schedules: await schedules.forReport(report.id, query.state, query.timezone) });
    }
  };

  const preview: RequestHandler = async (req, res) => {
    const report = await findReport(req.params.id, res);
    const body = report && parseInput(previewBody, req.body, res);
    if (!report || !body) {
      return;
    }
    try {
      res.json(await reports.preview(report, body));
    } catch (error) {
      if (error instanceof FormulaError) {
        sendProblem(res, 400, error.message);
      } else if (error instanceof z.ZodError) {
        sendProblem(res, 400, 'Invalid layout settings.');
      } else {
        throw error;
      }
    }
  };

  /** A handler for a YAML request body; documents the parsers refuse answer 400. */
  function fromYaml(handle: RenderReport): RequestHandler {
    return async (req, res) => {
      const report = await findReport(req.params.id, res);
      if (report) {
        await answerTextBody(
          res,
          req.body,
          'application/yaml',
          (text) => handle(report, text),
          isYamlRefusal,
        );
      }
    };
  }

  return Router()
    .get('/reports', read, listReports)
    .get('/reports/:id/schedules', read, listSchedules)
    .post('/reports/:id/preview', write, preview)
    .post(
      '/reports/:id/render',
      write,
      yamlBody,
      fromYaml((report, text) => reports.render(report, parseLayout(text))),
    )
    .post(
      '/reports/:id/remap',
      write,
      yamlBody,
      fromYaml(async (report, text) => ({
        rows: await reports.remap(report, parseColumnMapping(text)),
      })),
    );
}
