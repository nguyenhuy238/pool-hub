import { SafeImage } from "@/components/landing/SafeImage";
import type { AboutSettings } from "@/lib/api/landingSettingsApi";

export function AboutSection({ value }: { value: AboutSettings }) {
  if (!value.isEnabled) return null;
  return <section className="landing-section about-section" id="about">
    <div>
      <p className="eyebrow">{value.eyebrow}</p>
      <h2>{value.title}</h2>
      <p>{value.description}</p>
    </div>
    {value.imageUrl ? <SafeImage src={value.imageUrl} alt={value.title} /> : null}
  </section>;
}
