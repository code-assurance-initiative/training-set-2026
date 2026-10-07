import { formatDate } from '../../catalogue/format';
import { StorytimeRecording } from './StorytimeRecording';
import { UPCOMING_EVENTS } from './upcomingEvents';

export function EventsPage() {
  return (
    <div className="events">
      <h1>Events</h1>
      <p>All events are free. Booking is not needed unless an event says so.</p>

      <section aria-labelledby="upcoming-title">
        <h2 id="upcoming-title">Coming up</h2>
        <ul className="event-list">
          {UPCOMING_EVENTS.map((event) => (
            <li key={event.id} className="event-list__item">
              <h3>{event.title}</h3>
              <p>
                <time dateTime={`${event.date}T${event.time}`}>
                  {formatDate(event.date)}, {event.time}
                </time>{' '}
                · {event.branch} · {event.audience}
              </p>
            </li>
          ))}
        </ul>
      </section>

      <section aria-labelledby="recording-title">
        <h2 id="recording-title">Missed storytime?</h2>
        <img
          className="events__photo"
          src="/media/photos/storyteller.jpg"
          alt="A storyteller holding up a picture book in front of a group of seated children"
          width="480"
          height="320"
        />
        <p>
          Watch last week&apos;s session at home: “The Lighthouse Cat”, read by Priya from Central.
        </p>
        <StorytimeRecording
          title="Storytime, 30 September: The Lighthouse Cat"
          videoUrl="/media/video/storytime-2026-09-30.mp4"
          posterUrl="/media/photos/storytime-2026-09-30.jpg"
        />
      </section>
    </div>
  );
}
