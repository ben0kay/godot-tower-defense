// Shared world-space mask binding for effects and the faint terrain coverage.
uniform sampler2D reveal_mask : filter_linear, repeat_disable;
uniform vec4 reveal_bounds;
uniform bool reveal_enabled = false;
varying vec2 reveal_world_position;

vec2 sample_reveal() {
    vec2 uv = (reveal_world_position - reveal_bounds.xy) / max(reveal_bounds.zw, vec2(1.0));
    if (any(lessThan(uv, vec2(0.0))) || any(greaterThan(uv, vec2(1.0)))) {
        return vec2(0.0);
    }
    return texture(reveal_mask, uv).rg;
}
