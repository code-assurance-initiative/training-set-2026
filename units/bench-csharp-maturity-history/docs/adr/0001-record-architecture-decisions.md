# 1. Record architecture decisions

Date: 2026-01-05
Status: Accepted

## Context

ClinicScheduling is starting with a small team that will grow. Decisions about structure, frameworks and conventions
get made in conversations and pull-request threads, and are lost when the people who made them move on.

## Decision

We record every decision that shapes the structure of the code, or that a newcomer would otherwise have to rediscover,
as an Architecture Decision Record in `docs/adr/`, numbered in order, in the format of this document (status, context,
decision, consequences). A decision that is replaced is not deleted: its status becomes "Superseded by" the record that
replaces it.

## Consequences

- A decision is reviewed like code, in the pull request that introduces it.
- Changing a recorded decision means writing a new record, so the history of why the code looks as it does stays
  readable.
