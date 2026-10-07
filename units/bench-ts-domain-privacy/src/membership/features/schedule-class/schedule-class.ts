import { Router } from 'express';
import { z } from 'zod';
import { sendResult } from '../../../platform/http/problem.js';
import { requireScope, Scopes } from '../../../platform/http/scopes.js';
import { parseInput } from '../../../platform/http/validation.js';
import { ok, type Result } from '../../../shared-kernel/result.js';
import { ClassSession } from '../../domain/classes/class-session.js';
import { ClassSessionId } from '../../domain/classes/class-session-id.js';
import type { MembershipTransactions } from '../../domain/membership-transactions.js';

export const scheduleClassBody = z.object({
  title: z.string().min(1).max(120),
  startsAt: z.iso.datetime({ offset: true }),
  durationMinutes: z.int().min(15).max(240),
  capacity: z.int().min(1).max(200),
});

export type ScheduleClass = z.infer<typeof scheduleClassBody>;

export class ScheduleClassHandler {
  constructor(private readonly transactions: MembershipTransactions) {}

  async handle(command: ScheduleClass): Promise<Result<{ sessionId: string }>> {
    const session = new ClassSession(
      ClassSessionId.create(),
      command.title,
      new Date(command.startsAt),
      command.durationMinutes,
      command.capacity,
    );
    await this.transactions.run(({ sessions }) => sessions.save(session));
    return ok({ sessionId: session.id.value });
  }
}

export function scheduleClassRoutes(handler: ScheduleClassHandler): Router {
  return Router().post('/classes', requireScope(Scopes.classesWrite), async (req, res) => {
    const command = parseInput(scheduleClassBody, req.body, res);
    if (command) {
      const result = await handler.handle(command);
      sendResult(res, result, (created) => `/api/classes/${created.sessionId}`);
    }
  });
}
