"use client";
/* eslint-disable @next/next/no-img-element */

import { useState } from "react";

export function SafeImage({ src, alt, className }: { src?: string; alt: string; className?: string }) {
  const [failed, setFailed] = useState(false);
  return <img className={className} src={failed || !src ? "/images/poolhub/hero.png" : src} alt={alt} loading="lazy" onError={() => setFailed(true)} />;
}
