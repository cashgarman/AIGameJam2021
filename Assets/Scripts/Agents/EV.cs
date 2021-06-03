using UnityEngine;

public class EV : Agent<EV>
{
    [SerializeField] private Transform _pluginSlot;

    // Start is called before the first frame update
    public override Transform PluginSlot => _pluginSlot;
    public override string Name { get; }
    public override bool IsPluggedIn { get; set; }
    public override BaseAgent PluggedIn { get; set; }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
