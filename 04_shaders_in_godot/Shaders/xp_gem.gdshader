shader_type spatial;
render_mode cull_back, unshaded;

#include "includes/color_utils.gdshaderinc"

uniform vec3 gem_color : source_color = vec3(0.2, 1.0, 0.4);
uniform float bob_amplitude = 0.15;
uniform float bob_speed = 3.0;
uniform float glow_intensity : hint_range(0.0, 3.0) = 1.5;
uniform float pulse_speed = 2.0;
uniform float fresnel_power : hint_range(1.0, 8.0) = 2.0;

void vertex() {
	// Per-instance bob offset using instance position for variety
	float instance_offset = MODEL_MATRIX[3].x * 0.7 + MODEL_MATRIX[3].z * 1.3;
	float bob = sin(TIME * bob_speed + instance_offset) * bob_amplitude;
	VERTEX.y += bob;

	// Gentle pulse scale
	float scale_pulse = 1.0 + 0.1 * sin(TIME * pulse_speed + instance_offset * 0.5);
	VERTEX *= scale_pulse;
}

void fragment() {
	float p = pulse(TIME, pulse_speed);
	float brightness = glow_intensity * (0.8 + 0.2 * p);

	vec3 rim = fresnel_rim(NORMAL, VIEW, fresnel_power, vec3(1.0));
	vec3 color = gem_color * brightness + rim;

	ALBEDO = color;
	EMISSION = gem_color * brightness * 0.5;
}
