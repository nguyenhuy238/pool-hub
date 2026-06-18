export type MediaLibraryItem = {
  name: string;
  url: string;
  type: "image" | "video";
};

export const mediaLibrary: MediaLibraryItem[] = [
  { name: "PoolHub Hero", url: "/images/poolhub/hero.png", type: "image" },
  { name: "PoolHub Table Zone", url: "/images/poolhub/hero.png", type: "image" },
  { name: "PoolHub Gallery", url: "/images/poolhub/hero.png", type: "image" }
];
