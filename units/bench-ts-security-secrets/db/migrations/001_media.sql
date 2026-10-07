create table if not exists media (
  id           uuid primary key,
  owner_id     text        not null,
  object_key   text        not null unique,
  content_type text        not null,
  size_bytes   bigint      not null check (size_bytes >= 0),
  created_at   timestamptz not null default now()
);

create index if not exists media_owner_idx on media (owner_id);
