// World-anchored molten flow and sparse bubbles for the lava atlas entry.
shader_type canvas_item;
render_mode unshaded;

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

float spark(vec2 offset, vec2 direction, float age, float reach) {
    vec2 tip = direction * (2.0 + age * reach);
    return 1.0 - smoothstep(0.3, 1.35, length(offset - tip));
}

void fragment() {
    if (enabled) {
        vec2 p = world_position * 0.018;
        float clock = TIME * flow_speed;
        float warp = noise(p * 0.62 + vec2(-0.07, 0.05) * clock) - 0.5;
        float heat = noise(p * vec2(0.8, 1.45) + vec2(warp, -warp) * flow_strength
            - vec2(0.23, 0.11) * clock);
        float hot = smoothstep(0.57, 0.94, heat);
        // Only the SVG alpha is used during animation. Its cell-local brightness cannot restart at tile edges.
        vec3 molten = mix(vec3(0.55, 0.16, 0.028), vec3(0.78, 0.40, 0.09), heat);
        COLOR.rgb = molten + vec3(0.60, 0.36, 0.11) * hot * brightness_variation;

        if (bubble_density > 0.0 && bubble_intensity > 0.0) {
            vec2 patch = world_position / 44.0;
            vec2 cell = floor(patch);
            float seed = hash(cell + 19.0);
            float age = TIME * mix(0.14, 0.32, seed) + seed * 17.0;
            float cycle = floor(age);
            float phase = fract(age);
            float event_seed = hash(cell + vec2(cycle, -cycle));
            float onset = 0.08 + 0.46 * hash(cell + vec2(-cycle, cycle + 13.0));
            float duration = mix(0.13, 0.25, hash(cell + cycle + 31.0));
            float life = (phase - onset) / duration;
            if (event_seed < bubble_density && life > 0.0 && life < 1.0) {
                vec2 centre = 0.29 + 0.42 * vec2(hash(cell + cycle + 11.0), hash(cell - cycle + 51.0));
                vec2 offset = (fract(patch) - centre) * 44.0;
                float size = bubble_scale * mix(0.6, 1.0, seed);
                float burst_age = clamp((life - 0.63) / 0.37, 0.0, 1.0);
                float radius = mix(1.4, 4.0, smoothstep(0.0, 0.63, life)) * size;
                float distance_px = length(offset);
                float skin = 1.0 - smoothstep(radius * 0.40, radius + 0.65, distance_px);
                float bulge = skin * smoothstep(0.0, 0.35, life) * (1.0 - smoothstep(0.63, 0.70, life));
                float flash = skin * smoothstep(0.60, 0.65, life) * (1.0 - smoothstep(0.65, 0.78, life));
                float angle = hash(cell + cycle + 79.0) * 6.283185;
                vec2 direction = vec2(cos(angle), sin(angle));
                vec2 sideways = vec2(-direction.y, direction.x);
                float flecks = spark(offset, direction, burst_age, 4.8 * size);
                flecks += 0.6 * spark(offset, -direction * 0.6 + sideways * 0.8, burst_age, 3.2 * size);
                float spray = flecks * smoothstep(0.62, 0.68, life) * (1.0 - burst_age) * (1.0 - burst_age);
                float strength = bubble_intensity * mix(0.6, 1.6, hash(cell + cycle + 97.0));
                COLOR.rgb += vec3(1.0, 0.65, 0.21) * strength * (0.25 * bulge + flash + 0.85 * spray);
            }
        }
    }
}
