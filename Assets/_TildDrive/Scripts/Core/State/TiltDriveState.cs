namespace TiltDrive.Core.State
{
    [System.Serializable]
    public class TiltDriveState
    {
        public ControlState Control = new ControlState();
        public EngineState Engine = new EngineState();
        public TransmissionState Transmission = new TransmissionState();
        public MovementState Movement = new MovementState();
        public OtherState Other = new OtherState();
    }

    // =========================
    // CONTROL (INPUT USUARIO)
    // =========================
    [System.Serializable]
    public class ControlState
    {
        public float direccion;
        public float acelerador;
        public float freno;
        public float clutch;
        public bool frenoDeMano;

        public bool luces;

        public float camaraX;
        public float camaraY;
        public float camaraZ;
    }

    // =========================
    // MOTOR
    // =========================
    [System.Serializable]
    public class EngineState
    {
        public bool motorEncendido;
        public float rpm;
        public float torque;
        public float parMotor;
    }

    // =========================
    // TRANSMISIÓN
    // =========================
    [System.Serializable]
    public class TransmissionState
    {
        public int marchaActual;
        public float relacionActual;
        public bool tieneReversa;
    }

    // =========================
    // MOVIMIENTO
    // =========================
    [System.Serializable]
    public class MovementState
    {
        public float velocidadKmh;
        public float direccionActual;
    }

    // =========================
    // OTROS (EXPANSIÓN FUTURA)
    // =========================
    [System.Serializable]
    public class OtherState
    {
    }
}