#version 330 core
out vec4 FragColor;
in vec2 TexCoords;
uniform sampler2D u_Texture;
uniform int u_Horizontal;

void main()
{
    vec2 texSize = vec2(textureSize(u_Texture, 0));
    float weights[5] = float[](0.2270270270, 0.1945945946, 0.1216216216, 0.0540540541, 0.0162162162);
    vec2 dir = u_Horizontal != 0 ? vec2(1.0 / texSize.x, 0.0) : vec2(0.0, 1.0 / texSize.y);
    if (u_Horizontal != 0) {
        // H pass: non-premultiplied input — premultiply when accumulating
        vec4 s = texture(u_Texture, TexCoords);
        vec4 result = vec4(s.rgb * s.a, s.a) * weights[0];
        for (int i = 1; i < 5; i++) {
            s = texture(u_Texture, TexCoords + float(i) * dir);
            result += vec4(s.rgb * s.a, s.a) * weights[i];
            s = texture(u_Texture, TexCoords - float(i) * dir);
            result += vec4(s.rgb * s.a, s.a) * weights[i];
        }
        FragColor = result;
    } else {
        // V pass: premultiplied input from H pass — blend directly, no re-premultiplication
        vec4 result = texture(u_Texture, TexCoords) * weights[0];
        for (int i = 1; i < 5; i++) {
            result += texture(u_Texture, TexCoords + float(i) * dir) * weights[i];
            result += texture(u_Texture, TexCoords - float(i) * dir) * weights[i];
        }
        FragColor = result;
    }

    // restore lost transparency
    FragColor.a *= 2;
}
