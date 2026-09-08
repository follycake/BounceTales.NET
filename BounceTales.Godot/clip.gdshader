shader_type canvas_item;

instance uniform vec4 clip_rect;

void fragment() {
	vec2 p = FRAGCOORD.xy;
	if (p.x < clip_rect.x || p.x > clip_rect.x + clip_rect.z || p.y < clip_rect.y || p.y > clip_rect.y + clip_rect.w)
		discard;
}
