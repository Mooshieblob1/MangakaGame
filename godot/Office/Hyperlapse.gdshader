shader_type spatial;
render_mode unshaded, blend_mix, depth_draw_never, cull_back;

uniform vec4 cloth_color : source_color;
uniform vec4 skin_color : source_color;
uniform vec4 hair_color : source_color;
uniform bool hand_only = false;
varying float body_height;

void vertex() {
    body_height = VERTEX.y;
}

void fragment() {
    vec3 tint = mix(vec3(0.20, 0.24, 0.27), cloth_color.rgb,
        smoothstep(0.31, 0.42, body_height));
    tint = mix(tint, skin_color.rgb, smoothstep(0.66, 0.74, body_height));
    tint = mix(tint, hair_color.rgb, smoothstep(1.02, 1.13, body_height));
    ALBEDO = hand_only ? skin_color.rgb : tint;
    // Soft silhouette edges let close temporal samples merge into a streak.
    // Ordinary depth testing keeps the exposure behind desks and walls.
    float edge = pow(clamp(abs(dot(normalize(NORMAL), normalize(VIEW))), 0.0, 1.0), 0.8);
    ALPHA = COLOR.a * edge;
}
