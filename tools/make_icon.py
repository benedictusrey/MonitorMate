"""Generate the MonitorMate application icon from a small vector recipe."""
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "src" / "MonitorMate" / "Assets"
ASSETS.mkdir(parents=True, exist_ok=True)

S = 1024
img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
d = ImageDraw.Draw(img)

# Calm graphite tile.
d.rounded_rectangle((28, 28, S - 28, S - 28), radius=224, fill="#0E161E",
                    outline="#2A3B47", width=8)
# Subtle inner edge, kept as a solid value rather than a decorative gradient.
d.rounded_rectangle((48, 48, S - 48, S - 48), radius=206, outline="#1B2A34", width=4)

# Monitor body.
d.rounded_rectangle((206, 254, 818, 646), radius=52, fill="#13212A",
                    outline="#DCE8EC", width=38)
# Screen glass.
d.rounded_rectangle((256, 304, 768, 596), radius=24, fill="#0B141A")
# Rising sun.
d.ellipse((414, 386, 610, 582), fill="#F2B45C")
# Horizon line cuts the sun into a rising form.
d.rectangle((236, 510, 788, 548), fill="#0B141A")
d.line((256, 529, 768, 529), fill="#33505A", width=6)
# Stand.
d.rounded_rectangle((482, 644, 542, 728), radius=28, fill="#DCE8EC")
d.rounded_rectangle((374, 724, 650, 770), radius=23, fill="#DCE8EC")

img.save(ASSETS / "monitormate-1024.png")
sizes = [16, 20, 24, 32, 40, 48, 64, 128, 256]
img.save(ASSETS / "monitormate.ico", sizes=[(s, s) for s in sizes])
print(ASSETS / "monitormate.ico")
