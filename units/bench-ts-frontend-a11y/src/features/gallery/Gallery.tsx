import { useEffect, useState } from 'react';
import { IconButton } from '../../components/IconButton';
import { GALLERY_PHOTOS } from './galleryPhotos';

const ARROW_STEPS: Readonly<Partial<Record<string, number>>> = { ArrowLeft: -1, ArrowRight: 1 };

export function Gallery() {
  const [activeIndex, setActiveIndex] = useState<number | null>(null);
  const active = activeIndex === null ? undefined : GALLERY_PHOTOS[activeIndex];

  const viewerOpen = activeIndex !== null;

  const step = (offset: number) => {
    setActiveIndex((index) =>
      index === null ? null : (index + offset + GALLERY_PHOTOS.length) % GALLERY_PHOTOS.length,
    );
  };

  // While a photo is open, the arrow keys step through the set.
  useEffect(() => {
    if (!viewerOpen) {
      return;
    }
    const onKeyDown = (event: KeyboardEvent) => {
      const offset = ARROW_STEPS[event.key];
      if (offset !== undefined) {
        setActiveIndex((activeIndex + offset + GALLERY_PHOTOS.length) % GALLERY_PHOTOS.length);
      }
    };
    window.addEventListener('keydown', onKeyDown);
    return () => {
      window.removeEventListener('keydown', onKeyDown);
    };
  }, [viewerOpen]);

  return (
    <section className="gallery">
      <h1>Photo gallery</h1>
      <p>Moments from events and everyday life at our three branches.</p>
      <img
        className="gallery__ornament"
        src="/images/divider-waves.svg"
        alt=""
        role="presentation"
      />

      <ul className="gallery__grid">
        {GALLERY_PHOTOS.map((photo, index) => (
          <li key={photo.id}>
            <div
              className="gallery__thumb"
              onClick={() => {
                setActiveIndex(index);
              }}
            >
              <img src={photo.thumbnailUrl} alt={photo.description} width="240" height="160" />
            </div>
          </li>
        ))}
      </ul>

      {active && (
        <figure className="gallery__viewer">
          <img src={active.fullUrl} alt={active.description} width="960" height="640" />
          <figcaption>{active.caption}</figcaption>
          <div className="gallery__controls">
            <IconButton
              icon="previous"
              label="Previous photo"
              onClick={() => {
                step(-1);
              }}
            />
            <IconButton
              icon="next"
              label="Next photo"
              onClick={() => {
                step(1);
              }}
            />
            <IconButton
              icon="close"
              label="Close photo"
              onClick={() => {
                setActiveIndex(null);
              }}
            />
          </div>
        </figure>
      )}
    </section>
  );
}
