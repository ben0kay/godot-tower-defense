shader_type canvas_item;
render_mode blend_mix;
// Faint spatially varying shader nebula, with very slow motion and softened map edges.
uniform vec4 tint : source_color = vec4(0.22,0.38,0.64,1.0);
uniform float opacity : hint_range(0.0,1.0) = 0.12;
uniform float drift_speed : hint_range(0.0,0.1) = 0.002;
float hash(vec2 p) { return fract(sin(dot(p,vec2(127.1,311.7))) * 43758.5453); }
float noise2(vec2 p) {
    vec2 i=floor(p), f=fract(p);
    f=f*f*(3.0-2.0*f);
    return mix(mix(hash(i),hash(i+vec2(1.0,0.0)),f.x),mix(hash(i+vec2(0.0,1.0)),hash(i+vec2(1.0,1.0)),f.x),f.y);
}
void fragment() {
    vec2 p=UV*vec2(3.6,2.9)+vec2(TIME*drift_speed,TIME*drift_speed*0.37);
    float clouds=noise2(p)*0.72+noise2(p*2.1+vec2(3.2,1.6))*0.28;
    float feather=smoothstep(0.0,0.08,UV.x)*(1.0-smoothstep(0.92,1.0,UV.x))
        *smoothstep(0.0,0.08,UV.y)*(1.0-smoothstep(0.92,1.0,UV.y));
    COLOR=vec4(tint.rgb,opacity*tint.a*smoothstep(0.52,0.83,clouds)*feather);
}