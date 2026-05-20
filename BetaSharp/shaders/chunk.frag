#version 410

in vec4 vertexColor;
in vec2 texCoord;
in float fogDistance;

out vec4 FragColor;

uniform sampler2D textureSampler;
uniform vec4 fogColor;
uniform float fogDensity;
uniform float fogStart;
uniform float fogEnd;
uniform int fogMode;

uniform bool chunkFadeEnabled;
uniform float fadeProgress;

void main()
{
    vec4 texColor = texture(textureSampler, texCoord);
    vec4 finalColor = texColor * vertexColor;

    if (finalColor.a < 0.001)
    {
        discard;
    }

    float fogFactor;

    if (fogMode == 0)
    {
        fogFactor = (fogEnd - fogDistance) / (fogEnd - fogStart);
    }
    else
    {
        fogFactor = exp(-fogDensity * fogDistance);
    }

    fogFactor = clamp(fogFactor, 0.0, 1.0);

    vec4 fogAppliedColor = mix(fogColor, finalColor, fogFactor);

    if (chunkFadeEnabled)
    {
        FragColor = mix(fogColor, fogAppliedColor, fadeProgress);
    }
    else
    {
        FragColor = fogAppliedColor;
    }
}
