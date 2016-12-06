#include "Common.hlsl"

// Vertex shader for shadow mapping depth rendering stage
float4 Shadow_VS(VertexShaderInput vertex):SV_Position
{
    float4 result = (float4)0;
    vertex.Position.w = 1.0;
    result = mul(vertex.Position, WorldViewProjection);
    return result;
}