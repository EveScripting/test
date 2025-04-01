
extern alias SC;

using SC::SharedComponents.Py;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectCharacterCreation : DirectObject
    {
        //
        // This is not yet ready for use!
        // __builtin__.sm.services[cc]

        #region Fields

        #endregion Fields

        #region Constructors

        internal DirectCharacterCreation(DirectEve directEve) : base(directEve)
        {
            //
            // How do we detect we are at Character Creation?
            //
            //
            //carbonui.uicore.uicore.layer.charactercreation
            //I_charactercreation (or is that an L?)
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0]
            //ccContainer
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects 0,1,2,3,4,5,6
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0]
            //uiContainer
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1,2,3,4,5
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //empireStepContainer
            //
            //When on Step 3: Customizing the appearance of your character we have 0 - 3, not 0 - 4
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1,2,3
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //empireStepBannerHeader
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //buttonContainer
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2]
            //topViewCap
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[3
            //bottomViewCap
            //
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1,2,3,4
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //TechnologyStep or BloodLineStep or empireStepBannerHeader
            //
            //TechnologyStep - Step 1
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //uiContainer
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //leftSide - what is this?
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //rightSide - what is this?
            //
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //techCont
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //buttonContainer
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //raceBtn
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //techNavigationContainer
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1,2,3,4,5
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //leftArrowTechNavigationContainer
            //
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //techNavigationButtonContainer
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //techNavigationButton1
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2]
            //techNavigationButtonTitle1
            //text = SHIPS OF THE EMPIRE
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[3]
            //techNavigationButtonContainer2 - more here!
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[4]
            //techNavigationButtonContainer3 - more here!
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[5]
            //rightArrowTechNavigationContainer
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //techView1
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects 0 through 20
            //buttons showing what some ships do and such, not interesting....
            //
            //
            //
            //This is the same full path as TechnologyStep!
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //BloodlineStep - Step 2
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0, 1
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //uiContainer
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //leftSide
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //rightSide
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //bloodCont
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //bloodlineBanner
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects 0,1,2,3,4,5,6,7
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //bloodlineInfoCont
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //contentContainer
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1,2,3
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //cont_4
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //bloodlineBtn_4
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //Sprite
            //texturePath = res:/UI/Texture/classes/EmpireSelection/BloodlineIcons/bloodline_Brutor.png
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //bloodlineName_Brutor
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //cont_14
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects 0,1
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //bloodlineBtn_14
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //Sprite
            //texturePath = res:/UI/Texture/classes/EmpireSelection/BloodlineIcons/bloodline_Vherokior.png
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1]
            //bloodlineName_Vherokior
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2]
            //cont_3
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects 0,1
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
            //bloodlineBtn_14
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
            //Sprite
            //texturePath = res:/UI/Texture/classes/EmpireSelection/BloodlineIcons/bloodline_Sebiestor.png
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[1]
            //bloodlineName_Sebiestor
            //
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[3]
            //bloodlineTextCont
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[0]
            //bloodlineTextContCentered
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[0]
            //Label
            //BRUTOR
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[1]
            //Label
            //not useful to us...
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1]
            //boundingBoxWrapper_Bloodline4
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects 0,1
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //boundingBox_Bloodline4_Gender()
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //bloodlineSprite - not useful to us... Brutor_FS
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1]
            //bloodlineSprite - not useful to us... Brutor_FD
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2]
            //platformContainer
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
            //platformSprite
            //
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1]
            //boundingBox_Bloodlone4_Gender1
            //
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2]
            //boundingBoxWrapper_Bloodline14
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects 0,1
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
            //boundingBox_Bloodline14_Gender()
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
            //bloodlineSprite - not useful to us... Vherokior_FS
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1]
            //bloodlineSprite - not useful to us... Vherokior_FD
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2]
            //platformContainer
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
            //platformSprite
            //
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[1]
            //boundingBox_Bloodlone14_Gender1
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[3]
            //boundingBoxWrapper_Bloodline3
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects 0,1
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[0]
            //boundingBox_Bloodline3_Gender()
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[0]
            //bloodlineSprite - not useful to us... Sebiestor_FS
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[1]
            //bloodlineSprite - not useful to us... Sebiestor_FD
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[2]
            //platformContainer
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
            //platformSprite
            //
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[1]
            //boundingBox_Bloodlone3_Gender1
            //
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[4]
            //genderContainerParent
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0]
            //genderContainerParentCentered
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0]
            //genderContainer_4
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //GenderButton_F
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //normalSprite
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //hiliteSprite
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //genderLine_Bloodline4
            //
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2]
            //GenderButton_M
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects 0,1
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
            //normalSprite
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[1]
            //hiliteSprite
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[5]
            //genderContainerParent
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[5].children._childrenObjects[0]
            //genderContainerParentCentered
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[5].children._childrenObjects[0].children._childrenObjects[0]
            //genderContainer_14
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[5].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[5].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //GenderButton_F
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[5].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[5].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //normalSprite
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[5].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //hiliteSprite
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[5].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //genderLine_Bloodline14
            //
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[5].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2]
            //GenderButton_M
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[5].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects 0,1
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[5].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
            //normalSprite
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[5].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[1]
            //hiliteSprite
            //

            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[6]
            //genderContainerParent
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[6].children._childrenObjects[0]
            //genderContainerParentCentered
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[6].children._childrenObjects[0].children._childrenObjects[0]
            //genderContainer_3
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[6].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[6].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //GenderButton_F
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[6].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[6].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //normalSprite
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[6].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //hiliteSprite
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[6].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //genderLine_Bloodline3
            //
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[6].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2]
            //GenderButton_M
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[6].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects 0,1
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[6].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
            //normalSprite
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[6].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[1]
            //hiliteSprite
            //
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[7]
            //noteContainer
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[7].children._childrenObjects 0,1
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[7].children._childrenObjects[0]
            //noteContentContainer
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[7].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[7].children._childrenObjects[0].children._childrenObjects[0]
            //backButtonContainer - not useful to us...
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[7].children._childrenObjects[0].children._childrenObjects[1]
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[7].children._childrenObjects[1]
            //disclaimerCopntainer - not useful to us...
            //
            //
            //

            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //empireStepBannerHeader
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //headerTextBox
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //headerLabel
            //text = Choose an empire to explore
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2]
            //buttonContainer
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
            //bannersContainer
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects 0,1,2,3
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
            //empireBanner
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1,2,3,4
            //bannerHeader
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //headerTextBox
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //headerLabel
            //text = ""
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1]
            // empireBanner
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects 0,1,2,3,4\
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //bannerHeader
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1]
            //topPanelCap
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[2]
            //bottomPanelCap
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[3]
            //topBlackGradient
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[4]
            //BannerContents
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects 0,1,2,3,4,5
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[0]
            //raceSprite
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[1]
            //raceBpriteBg
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[2]
            //empireQuoteContainer
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[2].children._childrenObjects[0]
            //empireQuoteWrapper
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
            //empireQuitePhrase
            //text "<center>In our Caldari State, wealth and power comes only from loyalty to the Corporation" ...
            //
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1]
            //empireQuoteAttribution
            //text = "<center><b>Captain Karishal Muritor</b></center>"
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[3]
            //flareContainer
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[3].children._childrenObjects[0]
            //flareWrapper
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[0]
            //flare
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[4]
            //empireQuoteBackgroundContainer
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[4].children._childrenObjects 0,1
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[4].children._childrenObjects[0]
            //empireQuoteLineDecoration
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[4].children._childrenObjects[1]
            //lineDecorationCurvedGradientContainer
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[4].children._childrenObjects[5]
            //curvedGradientContainer
            //
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2]
            //empireBanner
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects 0,1,2,3,4
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
            //bannerHeader
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[1]
            //topPanelCap
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[2]
            //bottomPanelCap
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[3]
            //topBlackGradient
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[4]
            //BannerContents
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[4].children._childrenObjects 0,1,2,3,4,5
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[4].children._childrenObjects[0]
            //raceSprite
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[4].children._childrenObjects[1]
            //raceBpriteBg
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[4].children._childrenObjects[2]
            //empireQuoteContainer
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[4].children._childrenObjects[2].children._childrenObjects[0]
            //empireQuoteWrapper
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[4].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[4].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
            //empireQuitePhrase
            //text "<center>The federation os a beacon of democracy and liberty in the chaos of New Eden" ...
            //
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[4].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1]
            //empireQuoteAttribution
            //text = "<center><b>Captain Karishal Muritor</b></center>"
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[4].children._childrenObjects[3]
            //flareContainer
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[4].children._childrenObjects[3].children._childrenObjects[0]
            //flareWrapper
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[4].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[0]
            //flare
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[4].children._childrenObjects[4]
            //empireQuoteBackgroundContainer
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[4].children._childrenObjects[4].children._childrenObjects 0,1
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[4].children._childrenObjects[4].children._childrenObjects[0]
            //empireQuoteLineDecoration
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[4].children._childrenObjects[4].children._childrenObjects[1]
            //lineDecorationCurvedGradientContainer
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[4].children._childrenObjects[5]
            //curvedGradientContainer
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3]
            //empireBanner
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects 0,1,2,3,4
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[0]
            //bannerHeader
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[1]
            //topPanelCap
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[2]
            //bottomPanelCap
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[3]
            //topBlackGradient
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[4]
            //BannerContents
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[4].children._childrenObjects 0,1,2,3,4,5
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[4].children._childrenObjects[0]
            //raceSprite
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[4].children._childrenObjects[1]
            //raceBpriteBg
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[4].children._childrenObjects[2]
            //empireQuoteContainer
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[4].children._childrenObjects[2].children._childrenObjects[0]
            //empireQuoteWrapper
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[4].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[4].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
            //empireQuitePhrase
            //text "<center>Our mighty Empire is built upon our faith in the Creator and loyalty to the Throne" ...
            //
            //does not go here: text "<center>We will free all our people from the bonds of slavery" ...
            //
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[4].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1]
            //empireQuoteAttribution
            //text = "<center><b>Captain Karishal Muritor</b></center>"
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[4].children._childrenObjects[3]
            //flareContainer
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[4].children._childrenObjects[3].children._childrenObjects[0]
            //flareWrapper
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[4].children._childrenObjects[3].children._childrenObjects[0].children._childrenObjects[0]
            //flare
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[4].children._childrenObjects[4]
            //empireQuoteBackgroundContainer
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[4].children._childrenObjects[4].children._childrenObjects 0,1
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[4].children._childrenObjects[4].children._childrenObjects[0]
            //empireQuoteLineDecoration
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[4].children._childrenObjects[4].children._childrenObjects[1]
            //lineDecorationCurvedGradientContainer
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[3].children._childrenObjects[4].children._childrenObjects[5]
            //curvedGradientContainer

            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[3]
            //topViewCap
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[4]
            //bottomViewCap
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //topNavigationContainer - more here! - we shouldnt need it?
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2]
            //bottomNavigationContainer
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects 0,1
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
            //Container
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
            //EnlistButtonDecoration_Container
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //EnlistButtonDecoration
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1]
            //EnlistButton or NextStepButton
            //OnClick
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //EmpireThemedButton_ButtonContainer
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects 0,1,2,3,4,5,6
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //EmpireThemedButton_Button_Label
            //text = SELECT GALLENTE ORIGIN
            //text = SELECT CALDARI ORIGIN
            //text = SELECT AMARR ORIGIN
            //text = SELECT MINMATAR ORIGIN
            //text = CUSTOMIZE APPEARANCE //During step 2 Choosing your bloodline

            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[1]
            //EmpireThemedButton_Button_Outline
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[2]
            //EmpireThemedButton_Button_LineDecoration
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[3]
            //EmpireThemedButton_CurvedGradient
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[4]
            //EmpireThemedButton_Button_TintedGradient
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[5]
            //EmpireThemedButton_SideDecoration_TransformLeft
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[6]
            //EmpireThemedButton_SideDecoration_TransformRight
            //
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[1]
            //bottomNavigationWhiteLineContainer - more here! - we shouldnt need it? is this actually used?
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[3]
            //leftSide - nothing more here
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[4]
            //rightSide
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0]
            //buttonNavRight
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0]
            //finalizeButtonCC - visible? display?
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects 0,1
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //label
            //text = Enter Game
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[1]
            //underlay
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1]
            //saveButtonCC
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects 0,1
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //label
            //text = Finalize
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[1]
            //underlay
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[2]
            //nextButtonCC
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects 0,1
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[0]
            //label
            //text = Next
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[4].children._childrenObjects[0].children._childrenObjects[2].children._childrenObjects[1]
            //underlay


            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[5]
            //bottomLeftSide
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[5].children._childrenObjects[0]
            //buttonNavLeft
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[5].children._childrenObjects[0].children._childrenObjects[0]
            //backButtonCC - shouldnt need this?!
            //
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[1]
            //loadingWheel
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[2]
            //blackOutFill
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[3]
            //mainCont - this is empty on the empire choosing screen
            //
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[4] //when at empire choosing screen: Step 0?
            //backgroundGradient//when at empire choosing screen
            //
            //vignetteGradientSprite
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[5]
            //name = charCreationBackground_Gallente
            //name = charCreationBackground_Caldari
            //name = charCreationBackground_Amarr
            //name = charCreationBackground_Minmatar
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[6] //when at empire choosing screen: Step 0?
            //
            //charCreationBacjground_Global //when at empire choosing screen
            //backgroundGradient
            //

            //Step 0: Choosing your Empire
            //Step 1: Choosing your bloodline
            //Step 2: Customizing your appearance
            //Step 3: Naming your character
            //Step 4: Finalizing your character

            //
            //
            //Step 0:
            // Verify this is where we are:
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[6]
            //charCreationBacjground_Global - exists?
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[6].texturePath (string) contains Empire_Selection.png
            //
            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //empireStepBannerheader
            //
            //broken down:
            //carbonui.uicore.uicore.layer.charactercreation
            //.children._childrenObjects[0] // ccContainer
            //.children._childrenObjects[0] // uiContainer
            //.children._childrenObjects[0] // empireStepContainer
            //.children._childrenObjects[0] // empireStepBannerheader
            //
            //
            if (AtCharacterCreation)
            {
                if (!DirectEve.Layers.CharacterCreationLayer.Attribute("display").ToBool())
                {
                    if (DirectEve.Interval(10000)) Logging.Log.WriteLine("ChracterCreation: CharacterCreationLayer: display [false]");
                    return;
                }

                if (!DirectEve.Layers.CharacterCreationLayer.Attribute("isopen").ToBool())
                {
                    if (DirectEve.Interval(10000)) Logging.Log.WriteLine("ChracterCreation: CharacterCreationLayer: isopen [false]");
                    return;
                }

                //Full Path
                //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0] // ccContainer
                //
                //Path broken down
                //carbonui.uicore.uicore.layer.charactercreation // CharacterCreationLayer
                //.children._childrenObjects[0] // ccContainer
                // ccContainer
                //

                foreach (var thisPyObject_LookingFor_ccContainer in DirectEve.Layers.CharacterCreationLayer.Attribute("children").Attribute("_childrenObjects").ToList())
                {
                    var py_ccContainer = thisPyObject_LookingFor_ccContainer;
                    if (!py_ccContainer.IsValid)
                    {
                        if (DirectEve.Interval(10000)) Logging.Log.WriteLine("CharacterCreation: ccContainer not found");
                        continue;
                    }

                    if (py_ccContainer.Attribute("name").IsValid && py_ccContainer.Attribute("name").ToUnicodeString().ToLower() != "ccContainer".ToLower())
                    {
                        //if (DirectEve.Interval(10000, 10000, py_ccContainer.Attribute("name").ToUnicodeString())) Logging.Log.WriteLine("CharacterCreation: Name [" + py_ccContainer.Attribute("name").ToUnicodeString() + "] != ccContainer");
                        continue;
                    }
                    else
                    {
                        if (DirectEve.Interval(10000)) Logging.Log.WriteLine("CharacterCreation: ccContainer found");
                        //Full Path
                        //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0] // uiContainer

                        //broken down:
                        //carbonui.uicore.uicore.layer.charactercreation
                        //.children._childrenObjects[0] // ccContainer
                        //.children._childrenObjects[0]
                        // uiContainer

                        foreach (var thisPyObject_LookingFor_uiContainer00 in py_ccContainer.Attribute("children").Attribute("_childrenObjects").ToList())
                        {

                            var py_uiContainer = thisPyObject_LookingFor_uiContainer00;
                            if (!py_uiContainer.IsValid)
                            {
                                if (DirectEve.Interval(10000)) Logging.Log.WriteLine("CharacterCreation: uiContainer not found");
                                continue;
                            }

                            if (py_uiContainer.Attribute("name").IsValid && py_uiContainer.Attribute("name").ToUnicodeString().ToLower() != "uiContainer".ToLower())
                            {
                                //if (DirectEve.Interval(10000, 10000, py_uiContainer.Attribute("name").ToUnicodeString())) Logging.Log.WriteLine("CharacterCreation: Name [" + py_ccContainer.Attribute("name").ToUnicodeString() + "] != uiContainer");
                                continue;
                            }
                            else
                            {
                                if (DirectEve.Interval(10000)) Logging.Log.WriteLine("CharacterCreation: uiContainer found");
                                //Full Path
                                //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0] // empireStepContainer

                                //broken down:
                                //carbonui.uicore.uicore.layer.charactercreation
                                //.children._childrenObjects[0] // ccContainer
                                //.children._childrenObjects[0] // uiContainer
                                //.children._childrenObjects[0] // empireStepContainer

                                foreach (var thisPyObject_LookingFor_empiterStepContainer000 in py_uiContainer.Attribute("children").Attribute("_childrenObjects").ToList())
                                {
                                    //
                                    // Has in the list:
                                    // empireStepContainer
                                    // topNavigationContainer
                                    // bottomNavigationContainer
                                    // leftSide
                                    // rightSide
                                    // bottomLeftSide
                                    //
                                    var py_topNavigationContainer = thisPyObject_LookingFor_empiterStepContainer000;
                                    if (py_topNavigationContainer.IsValid)
                                    {
                                        if (py_topNavigationContainer.Attribute("name").IsValid && py_topNavigationContainer.Attribute("name").ToUnicodeString().ToLower() != "topNavigationContainer".ToLower())
                                        {
                                            //if (DirectEve.Interval(10000, 10000, py_topNavigationContainer.Attribute("name").ToUnicodeString())) Logging.Log.WriteLine("ChracterCreation: Name [" + py_topNavigationContainer.Attribute("name").ToUnicodeString() + "] != topNavigationContainer");
                                        }
                                        else
                                        {
                                            if (py_topNavigationContainer.Attribute("display").ToBool())
                                            {
                                                if (DirectEve.Interval(10000)) Logging.Log.WriteLine("CharacterCreation: topNavigationContainer found");
                                            }
                                        }
                                    }

                                    var py_bottomNavigationContainer = thisPyObject_LookingFor_empiterStepContainer000;
                                    if (py_bottomNavigationContainer.IsValid)
                                    {
                                        if (py_bottomNavigationContainer.Attribute("name").IsValid && py_bottomNavigationContainer.Attribute("name").ToUnicodeString().ToLower() != "bottomNavigationContainer".ToLower())
                                        {
                                            //if (DirectEve.Interval(10000, 10000, py_bottomNavigationContainer.Attribute("name").ToUnicodeString())) Logging.Log.WriteLine("ChracterCreation: Name [" + py_bottomNavigationContainer.Attribute("name").ToUnicodeString() + "] != bottomNavigationContainer");
                                        }
                                        else
                                        {
                                            if (py_bottomNavigationContainer.Attribute("display").ToBool())
                                            {
                                                if (DirectEve.Interval(10000)) Logging.Log.WriteLine("CharacterCreation: bottomNavigationContainer found");
                                            }
                                        }
                                    }

                                    var py_empireStepContainer = thisPyObject_LookingFor_empiterStepContainer000;
                                    if (py_empireStepContainer.IsValid)
                                    {
                                        if (py_empireStepContainer.Attribute("name").IsValid && py_empireStepContainer.Attribute("name").ToUnicodeString().ToLower() != "empireStepContainer".ToLower())
                                        {
                                            //if (DirectEve.Interval(10000, 10000, py_empireStepContainer.Attribute("name").ToUnicodeString())) Logging.Log.WriteLine("ChracterCreation: Name [" + py_empireStepContainer.Attribute("name").ToUnicodeString() + "] != empireStepContainer");
                                        }
                                        else
                                        {
                                            if (DirectEve.Interval(10000)) Logging.Log.WriteLine("CharacterCreation: empireStepContainer found");
                                            //Full Path
                                            //carbonui.uicore.uicore.layer.charactercreation.children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0] // empireStepContainer

                                            //broken down:
                                            //carbonui.uicore.uicore.layer.charactercreation
                                            //.children._childrenObjects[0] // ccContainer
                                            //.children._childrenObjects[0] // uiContainer
                                            //.children._childrenObjects[0] // empireStepContainer
                                            foreach (var thisPyObject_LookingFor_empiterStepContainer0000 in py_empireStepContainer.Attribute("children").Attribute("_childrenObjects").ToList())
                                            {
                                                //
                                                // In this list:
                                                // empireStepBannerheader
                                                // buttonContainer
                                                // topViewCap
                                                // bottomViewCap
                                                // EmpireStep
                                                //
                                                /**
                                                var py_topNavigationContainer = thisPyObject_LookingFor_empiterStepContainer000;
                                                if (py_topNavigationContainer.IsValid)
                                                {
                                                    if (py_topNavigationContainer.Attribute("name").IsValid && py_topNavigationContainer.Attribute("name").ToUnicodeString().ToLower() != "topNavigationContainer".ToLower())
                                                    {
                                                        //if (DirectEve.Interval(10000, 10000, py_topNavigationContainer.Attribute("name").ToUnicodeString())) Logging.Log.WriteLine("ChracterCreation: Name [" + py_topNavigationContainer.Attribute("name").ToUnicodeString() + "] != topNavigationContainer");
                                                    }
                                                    else
                                                    {
                                                        if (py_topNavigationContainer.Attribute("display").ToBool())
                                                        {
                                                            if (DirectEve.Interval(10000)) Logging.Log.WriteLine("CharacterCreation: topNavigationContainer found");
                                                        }
                                                    }
                                                }
                                                **/
                                            }
                                        }
                                    }

                                    continue;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (DirectEve.Interval(10000)) Logging.Log.WriteLine("ChracterCreation: Not at CharacterCreation");
            }
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        ///     The character selection screen is open
        /// </summary>
        public bool AtSelectRaceSelection => (bool) DirectEve.Layers.CharacterCreationLayer.Attribute("isopen");

        /// <summary>
        ///     The character creation screen is open
        /// </summary>
        public bool AtCharacterCreation => (bool)DirectEve.Layers.CharacterCreationLayer.Attribute("isopen") && (bool)DirectEve.Layers.CharacterCreationLayer.Attribute("display");

        /// <summary>
        ///     Is the character selection screen ready
        /// </summary>
        public bool IsCharacterCreationReady => (bool)DirectEve.Layers.CharacterCreationLayer.Attribute("ready");

        /// <summary>
        ///     Either the character selection screen or login screen is loading
        /// </summary>
        public bool IsLoading => (bool)DirectEve.Layers.CharacterCreationLayer.Attribute("isopening");

        /// <summary>
        ///     The server status string
        /// </summary>
        public string ServerStatus => (string)DirectEve.Layers.LoginLayer.Attribute("serverStatusTextControl").Attribute("text");


        #endregion Properties
    }
}