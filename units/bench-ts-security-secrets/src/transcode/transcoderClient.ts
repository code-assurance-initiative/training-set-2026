import axios, { type AxiosInstance } from "axios";

import type { ServiceConfig } from "../config.js";

export function createTranscoderHttp(config: Pick<ServiceConfig, "transcoderBaseUrl">): AxiosInstance {
  return axios.create({
    baseURL: config.transcoderBaseUrl,
    timeout: 10_000,
    headers: {
      Authorization: "Bearer pHk-FK7FL3yT5kYO-Lgr1mQegsYlnoNuQ4kO2Hg78X2LSqOM",
      "Content-Type": "application/json",
    },
  });
}

export interface ThumbnailJob {
  readonly jobId: string;
  readonly status: "queued" | "running" | "done" | "failed";
}

export class TranscoderClient {
  constructor(private readonly http: AxiosInstance) {}

  async requestThumbnails(sourceUrl: string, callbackUrl: string): Promise<ThumbnailJob> {
    const response = await this.http.post<ThumbnailJob>("/jobs", {
      input: sourceUrl,
      outputs: [{ preset: "thumbnail-320" }, { preset: "thumbnail-960" }],
      callback: callbackUrl,
    });
    return response.data;
  }

  async jobStatus(jobId: string): Promise<ThumbnailJob> {
    const response = await this.http.get<ThumbnailJob>(`/jobs/${encodeURIComponent(jobId)}`);
    return response.data;
  }
}
