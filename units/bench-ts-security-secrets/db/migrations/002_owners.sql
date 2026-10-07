create table if not exists owners (
  id                 text primary key,
  display_name       text not null,
  notify_email       text,
  stripe_customer_id text not null unique
);

alter table media
  add constraint media_owner_fk foreign key (owner_id) references owners (id);
