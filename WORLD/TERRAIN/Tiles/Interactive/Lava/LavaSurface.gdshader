// World-anchored molten flow and sparse bubbles for the lava atlas entry.
shader_type canvas_item;

uniform bool enabled = true;
uniform float flow_speed : hint_range(0.0, 2.0, 0.01) = 0.32;
uniform float flow_strength : hint_range(0.0, 2.0, 0.01) = 0.65;
uniform float brightness_variation : hint_range(0.0, 0.5, 0.01) = 0.18;
uniform float bubble_density : hint_range(0.0, 1.0, 0.01) = 0.20;
uniform float bubble_scale : hint_range(0.3, 1.5, 0.01) = 1.0;
uniform float bubble_intensity : hint_range(0.0, 1.0, 0.01) = 0.32;

varying vec2 world_position;

void vertex() {
    world_position = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

float hash(vec2 point) {
    vec3 p = fract(vec3(point.xyx) * 0.1031);
    p += dot(p, p.yzx + 33.33);
    return fract((p.x + p.y) * p.z);
}

float noise(vec2 point) {
    vec2 cell = floor(point);
    vec2 f = fract(point);
    vec2 w = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash(cell), hash(cell + vec2(1, 0)), w.x),
               mix(hash(cell + vec2(0, 1)), hash(cell + vec2(1, 1)), w.x), w.y);
}

void fragment() {
    if (enabled) {
        vec2 p = world_position * 0.018;
        float clock = TIME * flow_speed;
        float warp = noise(p * 0.62 + vec2(-0.07, 0.05) * clock) - 0.5;
        float heat = noise(p * vec2(0.8, 1.45) + vec2(warp, -warp) * flow_strength
            - vec2(0.23, 0.11) * clock);
        float hot = smoothstep(0.57, 0.94, heat);
        vec3 molten = mix(vec3(0.76, 0.28, 0.055), vec3(1.0, 0.61, 0.15), heat);
        COLOR.rgb = mix(molten, COLOR.rgb, 0.24) + vec3(1.0, 0.52, 0.12) * hot * brightness_variation;

        if (bubble_density > 0.0 && bubble_intensity > 0.0) {
            vec2 patch = world_position / 42.0;
            vec2 cell = floor(patch);
            float random = hash(cell + 19.0);
            float age = TIME * mix(0.14, 0.29, random) + random * 17.0;
            float cycle = floor(age);
            float phase = fract(age);
            if (hash(cell + vec2(cycle, -cycle)) < bubble_density && phase < 0.56) {
                vec2 centre = 0.27 + 0.46 * vec2(hash(cell + cycle + 11.0), hash(cell - cycle + 51.0));
                float distance_px = length(fract(patch) - centre) * 42.0;
                float life = phase / 0.56;
                float radius = mix(1.2, 4.4, life) * bubble_scale * mix(0.55, 1.0, random);
                float body = 1.0 - smoothstep(radius * 0.25, radius + 1.0, distance_px);
                float ring = 1.0 - smoothstep(0.2, 1.45, abs(distance_px - radius));
                float fade = smoothstep(0.0, 0.23, life) * (1.0 - smoothstep(0.65, 1.0, life));
                float bubble = mix(body, ring, smoothstep(0.53, 0.85, life));
                COLOR.rgb += vec3(1.0, 0.66, 0.24) * bubble * fade * bubble_intensity;
            }
        }
    }
}
