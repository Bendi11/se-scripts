
public class Radar: Screen {
    public override void Render() {
        Surface.WriteText("RADAR", false);
        foreach(var contact in ShipCore.I.Contacts) {
            Surface.WriteText("CONTACT", true);
        }
    }

    public override bool Handle(Input i) {
        return false;
    }
}
