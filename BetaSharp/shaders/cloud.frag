#version 410

in vec4 v_Color;
in vec2 v_TexCoord;
in vec3 v_LocalPos;
out vec4 FragColor;

uniform sampler2D u_Texture;
uniform float u_FogStart;
uniform float u_FogEnd;
uniform vec3 u_CloudOffset;
uniform float u_CloudScale;

const int minParalax = 4;
const int extraParalax = 32;
const int TextSize = 256;
const float CloudHeight = 2.0;
const float ScaledTextSize = CloudHeight / TextSize;
const float shadow = 0.66;

const int minGradient = 0;
const int extraGradient = 16;
const float GradientWidth = 0.5 / TextSize;

uniform int u_Quality;

void main()
{
    vec4 color = v_Color;
    vec3 normal = v_LocalPos + u_CloudOffset;

    float dist = length(normal.xz) * u_CloudScale / 2.0;
    float fogFactor = clamp((u_FogEnd - dist) / (u_FogEnd - u_FogStart), 0.0, 1.0);

    if (fogFactor < 0.001) discard;
    vec4 texColor;

    if (u_Quality <= 0)
    {
        texColor = texture(u_Texture, v_TexCoord);
        if (texColor.a < 0.1) discard;
    }
    else
    {
        // handel cases when the effect brakes
        if (normal.y > -3 && normal.y < 1) {
            FragColor = texture(u_Texture, v_TexCoord - normal.xz * (1.0 / TextSize));
            if (FragColor.a >= 0.1) {
                // player inside a cloud
                FragColor *= v_Color;
                if (normal.y < 0) return;
                normal.y *= 0.5;
                FragColor.rgb = FragColor.rgb * ((1 - normal.y) + shadow * (normal.y));
                return;
            } else {
                // player outide cloud but is same height.
                // as the effect breaks down at this height.
                if (normal.y < -1) color.a *= (-normal.y - 1) * 0.5;
                else if (normal.y < 0) discard;
                else color.a *= normal.y;
            }
        }

        texColor = texture(u_Texture, v_TexCoord);
        float len = length(normal);

        if (texColor.a < 0.1) {

            // flip expand direction when seen from above
            if (normal.y < 0) normal = -normal;

            int paralaxCount = int((fogFactor * fogFactor) * extraParalax) + minParalax;
            float paralaxStep = ScaledTextSize / paralaxCount;

            if (u_Quality <= 2) {
                for (int i = paralaxCount; i > 0; i--) {
                    texColor = texture(u_Texture, v_TexCoord + normal.xz / (len / (paralaxStep * i)));
                    if (texColor.a >= 0.1) break;
                }
            } else {

                const int densTarget = 4;

                float dens = 0;

                vec4 c = texColor;
                for (int i = paralaxCount; i > 0; i--) {
                    c = texture(u_Texture, v_TexCoord + normal.xz / (len / (paralaxStep * i)));
                    if (c.a >= 0.1) {
                        texColor = c;
                        if (++dens >= densTarget) break;
                    }
                }

                texColor.a *= dens / densTarget;
            }

            if (texColor.a < 0.1) discard;

        } else if (normal.y >= 0) {

            if (u_Quality <= 1) {
                texColor.rgb *= shadow;
            } else {
                // smooth gradient from highlight (sides) to shadow (underside)
                float gradiant = 0;

                int gradiantCount = int((fogFactor * fogFactor) * extraGradient) + minGradient;
                if (gradiantCount > 0) {
                    float gradiantStep = GradientWidth / gradiantCount;

                    for (int i = gradiantCount; i > 0; i--) {
                        if (texture(u_Texture, v_TexCoord - normal.xz / (len / (gradiantStep * i))).a < 0.1) gradiant += 1;
                    }

                    gradiant /= float(gradiantCount);

                    texColor.rgb = texColor.rgb * (gradiant + shadow * (1 - gradiant));

                    if (u_Quality >= 3) {
                        const int densTarget = 4;
                        const float gradiantStep = GradientWidth / densTarget;

                        int paralaxCount = int((fogFactor * fogFactor) * extraParalax) + minParalax;
                        float paralaxStep = ScaledTextSize / paralaxCount;

                        float dens = 0;

                        for (int i = densTarget; i > 0; i--) {
                            if (texture(u_Texture, v_TexCoord + normal.xz / (len / (paralaxStep * i))).a >= 0.1) {
                                if (++dens >= densTarget) break;
                            }
                        }

                        texColor.a *= dens / densTarget;
                    }
                } else {
                    texColor.rgb *= shadow;
                }
            }

        } else if (u_Quality >= 4) {
            const int densTarget = 4;
            const float gradiantStep = GradientWidth / densTarget;

            int paralaxCount = int((fogFactor * fogFactor) * extraParalax) + minParalax;
            float paralaxStep = ScaledTextSize / paralaxCount;

            float dens = 0;

            for (int i = densTarget; i > 0; i--) {
                if (texture(u_Texture, v_TexCoord - normal.xz / (len / (paralaxStep * i))).a >= 0.1) {
                    if (++dens >= densTarget) break;
                }
            }

            texColor.a *= dens / densTarget;
        }
    }

    FragColor = texColor * color;
    FragColor.a *= fogFactor;
}
