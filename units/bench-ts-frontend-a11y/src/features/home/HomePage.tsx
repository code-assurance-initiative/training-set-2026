import { AppLink } from '../../components/AppLink';
import { hrefFor } from '../../routing';
import { OPENING_HOURS, WEEKDAYS } from './openingHours';

export function HomePage() {
  return (
    <>
      <section className="hero" aria-labelledby="hero-title">
        <video
          className="hero__video"
          src="/media/video/reading-room-loop.mp4"
          poster="/media/photos/reading-room-poster.jpg"
          autoPlay
          loop
          muted
          playsInline
        />
        <div className="hero__content">
          <h1 id="hero-title" className="hero__title">
            Your library, open to everyone
          </h1>
          <p className="hero__lead">
            Borrow books, audiobooks, films and magazines from three branches — free with a library
            card.
          </p>
          <AppLink className="button button--primary" href={hrefFor('search')}>
            Search the catalogue
          </AppLink>
        </div>
      </section>

      <section className="home-section" aria-labelledby="visit-title">
        <h2 id="visit-title">Visit us</h2>
        <div className="home-section__media">
          <img
            className="home-section__photo"
            src="/media/photos/IMG_2041.jpg"
            alt="IMG_2041.jpg"
            width="480"
            height="320"
          />
          <img
            className="home-section__photo"
            src="/media/photos/story-corner.jpg"
            alt="Children reading on floor cushions in the story corner at Central Library"
            width="480"
            height="320"
          />
        </div>
        <p>
          The reading room at Central Library has quiet study desks, newspapers and free wifi. Every
          branch has a children&apos;s corner with weekly storytimes.
        </p>
        <p>
          Opening hours change over the school holidays. For this week&apos;s hours,{' '}
          <a href="#opening-hours">click here</a>.
        </p>
      </section>

      <section className="home-section" aria-labelledby="whats-on-title">
        <h2 id="whats-on-title">What&apos;s on</h2>
        <p>
          Storytimes, coding clubs for teenagers and a monthly local-history talk.{' '}
          <AppLink href={hrefFor('events')}>See all events</AppLink>
        </p>
      </section>

      <section className="home-section" id="opening-hours" aria-labelledby="hours-title">
        <h2 id="hours-title">Opening hours this week</h2>
        <table className="hours-table">
          <caption className="visually-hidden">Opening hours by branch and weekday</caption>
          <thead>
            <tr>
              <th scope="col">Branch</th>
              {WEEKDAYS.map((day) => (
                <th key={day} scope="col">
                  {day}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {OPENING_HOURS.map((branch) => (
              <tr key={branch.name}>
                <th scope="row">{branch.name}</th>
                {branch.hours.map((hours, index) => (
                  <td key={WEEKDAYS[index]}>{hours ?? 'Closed'}</td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </section>
    </>
  );
}
