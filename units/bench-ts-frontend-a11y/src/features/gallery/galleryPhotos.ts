export interface GalleryPhoto {
  readonly id: string;
  readonly thumbnailUrl: string;
  readonly fullUrl: string;
  readonly description: string;
  readonly caption: string;
}

export const GALLERY_PHOTOS: readonly GalleryPhoto[] = [
  {
    id: 'spring-book-sale',
    thumbnailUrl: '/media/gallery/spring-book-sale-thumb.jpg',
    fullUrl: '/media/gallery/spring-book-sale.jpg',
    description: 'Volunteers sorting donated books into crates on trestle tables',
    caption: 'Spring book sale, Central Library',
  },
  {
    id: 'coding-club',
    thumbnailUrl: '/media/gallery/coding-club-thumb.jpg',
    fullUrl: '/media/gallery/coding-club.jpg',
    description: 'Teenagers at laptops building a small robot with a librarian',
    caption: 'Thursday coding club, Harbour Branch',
  },
  {
    id: 'history-talk',
    thumbnailUrl: '/media/gallery/history-talk-thumb.jpg',
    fullUrl: '/media/gallery/history-talk.jpg',
    description: 'A speaker pointing at a projected 1911 map of the harbour to a seated audience',
    caption: 'Local-history talk: the mill fire of 1911',
  },
  {
    id: 'reading-garden',
    thumbnailUrl: '/media/gallery/reading-garden-thumb.jpg',
    fullUrl: '/media/gallery/reading-garden.jpg',
    description: 'Benches among raised flower beds behind Hillside Branch',
    caption: 'The new reading garden at Hillside Branch',
  },
];
