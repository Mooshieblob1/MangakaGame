using System;
using System.Linq;
using Godot;

namespace MangakaGame;

public partial class OfficeView
{
    private ColorRect? _toiletPrivacy;
    private ShaderMaterial? _toiletPrivacyMaterial;
    internal bool ToiletPrivacyVisible=>_toiletPrivacy?.Visible==true;

    private void BuildToiletPrivacy()
    {
        // A canvas pass blurs the actual rendered occupant and nearby WC fittings.
        // https://docs.godotengine.org/en/stable/tutorials/shaders/screen-reading_shaders.html
        var shader=new Shader{Code="""
            shader_type canvas_item;
            uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear_mipmap;
            uniform float blur_lod = 4.0;
            void fragment() {
                vec3 blurred = textureLod(screen_texture, SCREEN_UV, blur_lod).rgb;
                vec2 edge = min(UV, vec2(1.0) - UV);
                float mask = smoothstep(0.0, 0.045, min(edge.x, edge.y));
                COLOR = vec4(blurred, mask);
            }
            """};
        _toiletPrivacyMaterial=new ShaderMaterial{Shader=shader};
        _toiletPrivacy=new ColorRect{Name="WCPrivacyBlur",Material=_toiletPrivacyMaterial,
            MouseFilter=MouseFilterEnum.Ignore,Visible=false};_viewport.AddChild(_toiletPrivacy);
    }
    private void UpdateToiletPrivacy()
    {
        if(_toiletPrivacy is null||_camera is null)return;
        var occupant=_actors.Values.Concat(_ambient).FirstOrDefault(a=>a.Visible&&a.PrivacyObscured);
        if(occupant is null||_camera.IsPositionBehind(occupant.GlobalPosition))
        {_toiletPrivacy.Hide();return;}
        var lower=new Vector2(float.PositiveInfinity,float.PositiveInfinity);
        var upper=new Vector2(float.NegativeInfinity,float.NegativeInfinity);
        foreach(var x in new[]{-.43f,.43f})foreach(var y in new[]{-.08f,1.65f})foreach(var z in new[]{-.43f,.43f})
        {
            var point=_camera.UnprojectPosition(occupant.GlobalPosition+new Vector3(x,y,z));
            lower=new(Math.Min(lower.X,point.X),Math.Min(lower.Y,point.Y));
            upper=new(Math.Max(upper.X,point.X),Math.Max(upper.Y,point.Y));
        }
        _toiletPrivacy.Position=lower;_toiletPrivacy.Size=upper-lower;
        _toiletPrivacyMaterial!.SetShaderParameter("blur_lod",(float)Math.Clamp(Math.Log2(Math.Max(1,(upper-lower).Y*.014)),1.5,4));
        _toiletPrivacy.Show();
    }
}
