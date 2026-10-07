export interface FetchCall {
  readonly url: string;
  readonly init: RequestInit | undefined;
}

/** A fetch stand-in that records calls and answers each with the next queued response. */
export function fakeFetch(...responses: Response[]) {
  const calls: FetchCall[] = [];
  const fetchImpl = (input: string | URL | Request, init?: RequestInit): Promise<Response> => {
    calls.push({ url: input instanceof Request ? input.url : String(input), init });
    const response = responses.shift();
    return response ? Promise.resolve(response) : Promise.reject(new Error('unexpected request'));
  };
  return { fetchImpl, calls };
}
