// Shared color utility functions for SwarmSurvivor shaders

vec3 fresnel_rim(vec3 normal, vec3 view, float power, vec3 rim_color) {
	float rim = 1.0 - max(dot(normal, view), 0.0);
	rim = pow(rim, power);
	return rim_color * rim;
}

vec3 apply_damage_flash(vec3 base_color, float flash_intensity) {
	return mix(base_color, vec3(1.0), flash_intensity);
}

float pulse(float time, float speed) {
	return 0.5 + 0.5 * sin(time * speed);
}
