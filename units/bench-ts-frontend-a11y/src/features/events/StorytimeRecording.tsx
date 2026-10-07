interface StorytimeRecordingProps {
  readonly title: string;
  readonly videoUrl: string;
  readonly posterUrl: string;
}

/** The recording of a past storytime session, for families who could not come. */
export function StorytimeRecording({ title, videoUrl, posterUrl }: StorytimeRecordingProps) {
  return (
    <figure className="recording">
      <video
        className="recording__video"
        controls
        preload="metadata"
        poster={posterUrl}
        width="640"
        height="360"
      >
        <source src={videoUrl} type="video/mp4" />
        <p>
          Your browser cannot play this video. <a href={videoUrl}>Download the recording</a>.
        </p>
      </video>
      <figcaption>{title}</figcaption>
    </figure>
  );
}
