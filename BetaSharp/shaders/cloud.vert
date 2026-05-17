#version 410

layout(location = 0) in vec3 a_Position;
layout(location = 1) in vec4 a_Color;
layout(location = 2) in vec2 a_TexCoord;

out vec4 v_Color;
out vec2 v_TexCoord;
out vec3 v_LocalPos;

uniform mat4 u_ModelView;
uniform mat4 u_Projection;
uniform mat4 u_TextureMatrix;
uniform vec3 u_CloudOffset;

void main()
{
    v_Color = a_Color;
    v_TexCoord = (u_TextureMatrix * vec4(a_TexCoord, 0.0, 1.0)).xy;
    v_LocalPos = a_Position;
    gl_Position = u_Projection * u_ModelView * vec4(a_Position, 1.0);
}
