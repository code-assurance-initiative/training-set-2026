import { randomBytes, randomUUID } from 'node:crypto';
import { Router } from 'express';
import { z } from 'zod';
import { sendProblem } from '../http/problem.js';
import { requireScope, Scopes } from '../http/scopes.js';
import { parseInput } from '../http/validation.js';
import type { ReportDefinition } from '../reports/report-model.js';
import type { CrmClient } from './crm-client.js';
import type { ReportMailer } from './report-mailer.js';
import type { SubscriptionCollection } from './subscription.js';

const subscribeBody = z.strictObject({ email: z.email().max(254) });
const channelBody = z.strictObject({ enabled: z.boolean() });

export interface SubscriptionRouteDependencies {
  readonly subscriptions: SubscriptionCollection;
  readonly reports: { get(id: string): Promise<ReportDefinition | undefined> };
  readonly mailer: Pick<ReportMailer, 'deliver'>;
  readonly crm: Pick<CrmClient, 'setEmailChannel'>;
  readonly publicBaseUrl: string;
  readonly now: () => Date;
}

export function subscriptionRoutes(deps: SubscriptionRouteDependencies) {
  const { subscriptions, reports, mailer, crm, publicBaseUrl, now } = deps;
  const admin = requireScope(Scopes.subscriptionsAdmin);

  return Router()
    .post('/reports/:id/subscriptions', admin, async (req, res) => {
      const reportId = parseInput(z.uuid(), req.params.id, res);
      const body = reportId === undefined ? undefined : parseInput(subscribeBody, req.body, res);
      if (reportId === undefined || !body) {
        return;
      }
      const email = body.email.toLowerCase();
      const existing = await subscriptions.findOne({ email, reportId });
      if (existing) {
        res.json({ id: existing._id });
        return;
      }
      const id = randomUUID();
      await subscriptions.insertOne({
        _id: id,
        reportId,
        email,
        unsubscribeToken: randomBytes(24).toString('base64url'),
        createdAt: now(),
      });
      res.status(201).json({ id });
    })
    .get('/reports/:id/subscriptions', admin, async (req, res) => {
      const reportId = parseInput(z.uuid(), req.params.id, res);
      if (reportId === undefined) {
        return;
      }
      const found = await subscriptions
        .find({ reportId }, { projection: { unsubscribeToken: 0 } })
        .toArray();
      res.json({
        subscriptions: found.map(({ _id, email, createdAt }) => ({ id: _id, email, createdAt })),
      });
    })
    .post('/reports/:id/deliveries', admin, async (req, res) => {
      const reportId = parseInput(z.uuid(), req.params.id, res);
      const report = reportId === undefined ? undefined : await reports.get(reportId);
      if (reportId === undefined) {
        return;
      }
      if (!report) {
        sendProblem(res, 404, 'No report has this id.');
        return;
      }
      const recipients = await subscriptions.find({ reportId }).toArray();
      const summary = await mailer.deliver(
        { id: report.id, name: report.name, link: `${publicBaseUrl}/reports/${report.id}` },
        recipients,
      );
      res.json(summary);
    })
    .put('/subscriptions/:id/channels/email', admin, async (req, res) => {
      const id = parseInput(z.uuid(), req.params.id, res);
      const body = id === undefined ? undefined : parseInput(channelBody, req.body, res);
      if (id === undefined || !body) {
        return;
      }
      await crm.setEmailChannel(id, body.enabled);
      res.status(204).end();
    });
}
