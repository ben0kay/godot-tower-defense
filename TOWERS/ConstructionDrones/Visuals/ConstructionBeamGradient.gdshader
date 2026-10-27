shader_type canvas_item;
render_mode blend_mix, unshaded;

// Cheap, static GPU falloff over one cached four-vertex beam polygon.
// UV.x spans the tapered beam width; UV.y runs from the nozzle to the work site.
// No TIME, noise, texture sampling, extra particles, or per-drone shader parameters.
void fragment() {
    float side = abs(UV.x * 2.0 - 1.0);
    // Derivative-aware feather smooths the polygon boundary across on-screen pixels.
    float antialias = fwidth(side) * 0.7;
    float feather = 1.0 - smoothstep(0.48 - antialias, 1.0 + antialias, side);
    // Brighter inner beam blends continuously into the surrounding field, with no separate hard-edged core.
    float core = 1.0 - smoothstep(0.0, 0.62, side);
    float intensity = feather * (0.60 + 0.40 * core);
    COLOR.a *= intensity;
}
