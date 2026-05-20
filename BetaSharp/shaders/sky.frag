#version 410

in vec4 v_Color;
in vec2 v_TexCoord;
in vec3 v_LocalPos;
in float v_FogDist;
out vec4 FragColor;

uniform sampler2D u_Texture;
uniform int u_UseTexture;
uniform float u_FogStart;
uniform float u_FogEnd;
uniform int u_GradientMode;
uniform vec3 u_SkyColor;
uniform vec3 u_GroundColor;

//const float PI = 3.14159265359;
//const float PIH = 1.5707963268;
const float PIH2 = 0.7853981634;

void main()
{
    float fogRange = max(u_FogEnd - u_FogStart, 0.001);
    float fogFactor = clamp((u_FogEnd - v_FogDist) / fogRange, 0.0, 1.0);

    vec4 color;

    if (u_UseTexture != 0)
    {
        color = texture(u_Texture, v_TexCoord) * v_Color;
    }
    else if (u_GradientMode != 0)
    {
        float horizDist = length(v_LocalPos.xz);
        float elevation = atan(v_LocalPos.y, max(horizDist, 0.001));
        // elevation: PI/2=zenith, 0=horizon, -PI/2=nadir
        // (elevation / (PI * 0.5) + 1.0) * 0.5
        float t = clamp(elevation / PIH2 + 0.5, 0.0, 1.0);
        vec3 gradColor = mix(u_GroundColor, u_SkyColor, t);
        color = vec4(gradColor, fogFactor);
    }
    else
    {
        color = v_Color;
        color.a *= fogFactor;
    }

    if (color.a < 0.001)
    discard;
    FragColor = color;
}
