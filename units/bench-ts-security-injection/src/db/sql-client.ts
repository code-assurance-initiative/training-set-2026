/** The part of a node-postgres pool the repositories use; a `pg.Pool` satisfies it. */
export interface SqlClient {
  query<Row extends object>(
    text: string,
    values?: readonly unknown[],
  ): Promise<{ readonly rows: Row[]; readonly rowCount: number | null }>;
}
