using UnityEngine;
using Zenject;

public class UntitledInstaller : MonoInstaller
{
    public SkillManager skillManager;

    public override void InstallBindings()
    {
        Container.Bind<ISkillManager>().FromInstance(skillManager);
    }
}