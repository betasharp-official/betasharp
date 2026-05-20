#version 410

layout(location = 0) in vec3 a_Position;
layout(location = 1) in vec4 a_Color;
layout(location = 2) in vec2 a_TexCoord;

out vec4 v_Color;
out vec2 v_TexCoord;
out vec3 v_LocalPos;
out float v_FogDist;

uniform mat4 u_ModelView;
uniform mat4 u_Projection;

void main()
{
    v_Color = a_Color;
    v_TexCoord = a_TexCoord;
    v_LocalPos = a_Position;
    v_FogDist = length(a_Position.xz);
    gl_Position = u_Projection * u_ModelView * vec4(a_Position, 1.0);
}
