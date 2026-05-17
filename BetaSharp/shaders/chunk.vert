#version 410

layout (location = 0) in vec3 inPosition;
layout (location = 1) in uvec2 inUV;
layout (location = 2) in vec4 inColor;
layout (location = 3) in uint inLight;

out vec4 vertexColor;
out vec2 texCoord;
out float fogDistance;

uniform mat4 modelViewMatrix;
uniform mat4 projectionMatrix;
uniform vec2 chunkPos;
uniform float time;
uniform bool envAnim;

const float POSITION_SCALE_INV = 64.0 / 32767.0;

vec3 unpackPosition(vec3 packedPos)
{
    return packedPos * POSITION_SCALE_INV;
}

float unpackSkyLight(uint light)
{
    return float((light >> 4) & 0xFu) / 15.0;
}

float unpackBlockLight(uint light)
{
    return float(light & 0xFu) / 15.0;
}

int atlasIndexFromUV(vec2 uv)
{
    uv = clamp(uv, 0.0, 0.999999);

    ivec2 tile = ivec2(floor(uv * 16.0));

    return tile.x + tile.y * 16;
}

const float WAVY_STRENGTH = 1.0;

vec2 calcWave(in vec3 pos)
{
    float pi2wt = 2.0 * 3.14159265 * time;
    float magnitude = abs(sin(dot(vec4(time, pos), vec4(1.0, 0.005, 0.005, 0.005))) * 0.5 + 0.72) * 0.013;
    vec2 ret = (sin(pi2wt * vec2(0.0063, 0.0015) * 4.0 - pos.xz + pos.y * 0.05) + 0.1) * magnitude;
    return ret;
}

vec3 calcMovePlants(in vec3 pos)
{
    vec2 move1 = calcWave(pos);
    float move1y = -length(move1);
    return vec3(move1.x, move1y, move1.y) * 5.0 * WAVY_STRENGTH;
}

vec3 calcWaveLeaves(in vec3 pos)
{
    float pi2wt = 2.0 * 3.14159265 * time;
    float magnitude = abs(sin(dot(vec4(time, pos), vec4(1.0, 0.005, 0.005, 0.005))) * 0.5 + 0.72) * 0.013;
    vec3 ret = sin(pi2wt * vec3(0.0063, 0.0224, 0.0015) * 1.5 - pos) * magnitude;
    return ret;
}

vec3 calcMoveLeaves(in vec3 pos)
{
    vec3 move1 = calcWaveLeaves(pos) * vec3(1.0, 0.2, 1.0);
    return move1 * 5.0 * WAVY_STRENGTH;
}

bool isLeaf(int idx)
{
    return idx == 52 || idx == 132;
}

bool isPlant(int idx)
{
    return idx == 12 || idx == 13 || idx == 39 || idx == 55 || idx == 56;
}

void applyWaving(inout vec3 worldPos, in vec3 position, in int textureIndex, in float waviness)
{
    if (isLeaf(textureIndex))
    {
        worldPos += calcMoveLeaves(worldPos) * waviness;
    }
    else if (isPlant(textureIndex))
    {
        worldPos += calcMovePlants(worldPos) * max(waviness, 0.5);
    }
}

void main()
{
    vec3 position = unpackPosition(inPosition);
    vec2 uv = vec2(inUV & 0x7FFFu) / 32767.0;
    uvec2 signBits = (inUV >> 15u) & 1u;

    const float epsilon = 1.0 / 65536.0;
    vec2 bias = vec2(
    (signBits.x == 0u) ? epsilon : -epsilon,
    (signBits.y == 0u) ? epsilon : -epsilon
    );

    uv += bias;

    if (envAnim)
    {
        int textureIndex = atlasIndexFromUV(uv);
        vec3 worldPos = position + vec3(chunkPos.x, 0.0, chunkPos.y);

        applyWaving(worldPos, position, textureIndex, 1.0);

        position = worldPos - vec3(chunkPos.x, 0.0, chunkPos.y);
    }

    vec4 color = inColor;

    vec4 viewPos = modelViewMatrix * vec4(position, 1.0);
    gl_Position = projectionMatrix * viewPos;

    vertexColor = color;
    texCoord = uv;

    fogDistance = length(viewPos.xyz);
}
