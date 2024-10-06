
using UnityEngine;
using DG.Tweening;


public class Switch : MonoBehaviour
{
    [SerializeField]private bool _isRight=true;
    private Rigidbody2D _rigidbody;
    [SerializeField] private int switchID;

    private void Awake()
    {
       
       if (!PlayerPrefs.HasKey(transform.parent.name))
       {
           PlayerPrefs.SetInt(transform.parent.name, _isRight ? 1 : 0);
           if (_isRight)
           {
               RotateSwitch();
           }
           else
           {
               RotateSwitch();
           }
       }
       else if (PlayerPrefs.GetInt(transform.parent.name) == 0)
       {
           _isRight = false;
           RotateSwitch();
       }
       else if (PlayerPrefs.GetInt(transform.parent.name) == 1)
       {
           _isRight = true;
           RotateSwitch();
       }
    }
    

    public void SwitchChange(bool dir)
    {
        if (dir==_isRight)
        {  
           
             _isRight = !_isRight;
             PlayerPrefs.SetInt(transform.parent.name, _isRight ? 1 : 0);
             RotateSwitch();
            GameManager.instance.SwitchChange(switchID);
        }
    }

    public void PlayerTrigger()
    {
        _isRight = !_isRight;
        RotateSwitch();
        PlayerPrefs.SetInt(transform.parent.name, _isRight ? 1 : 0);
        GameManager.instance.SwitchChange(switchID);
    }


    private void RotateSwitch()
    {
        transform.DOLocalRotate(new Vector3(0,0,40*(_isRight?-1:1)),0.3f, RotateMode.Fast);
    }
}
