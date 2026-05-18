#version 330 core
out vec4 FragColor;
in vec2 TexCoords;
uniform sampler2D screenTexture;
uniform float gamma;

void main()
{
    vec4 col = texture(screenTexture, TexCoords);
    vec3 washedOutColor = pow(col.rgb, vec3(1.0 / gamma));
    FragColor = vec4(washedOutColor, col.a);
}
