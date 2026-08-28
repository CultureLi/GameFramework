Shader "Custom/ReusePass"
{
    Properties
    {
        _Color("Color", Color) = (0,1,0,1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        UsePass "Custom/SimpleColor/Forward"
    }
}