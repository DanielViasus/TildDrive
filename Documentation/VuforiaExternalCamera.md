# Vuforia external camera screen

Esta implementacion conecta una escena movil con Vuforia a una pantalla 3D dentro del simulador.

## Flujo

1. El telefono ejecuta una escena Unity con Vuforia.
2. En esa escena se agrega `VuforiaMjpegBroadcaster`.
3. El broadcaster captura la salida final de pantalla, incluyendo camara y aumentos de Vuforia, y publica un stream MJPEG.
4. El simulador agrega `ExternalCameraScreen` y consume ese MJPEG como textura de una pantalla 3D.

## Telefono con Vuforia

1. Abre el proyecto/escena movil donde ya esta configurado Vuforia.
2. Agrega `VuforiaMjpegBroadcaster` a un GameObject activo.
3. Deja `Capture Source` en `Final Screen` para transmitir exactamente lo que se ve en el telefono.
4. Ajusta `Port`, `Frames Per Second`, `Jpeg Quality` y `Max Frame Width` segun la red.
5. Compila al telefono. El telefono y el PC del simulador deben estar en la misma red Wi-Fi.
6. Al iniciar la escena, abre en el PC `http://IP_DEL_TELEFONO:8080/vuforia.mjpg` para confirmar que hay video.

Notas:

- En Android, el build necesita permiso de red. En Unity normalmente basta con que `Internet Access` este en `Require`.
- Si el stream se siente pesado, baja `Frames Per Second`, `Jpeg Quality` o `Max Frame Width`.
- Si quieres capturar una camara especifica en vez de toda la pantalla, cambia `Capture Source` a `Camera Render` y asigna `Capture Camera`.

## Simulador

1. Abre la escena del simulador.
2. Usa el menu `TiltDrive > External Camera > Create Vuforia Screen`, o crea un GameObject y agrega `ExternalCameraScreen`.
3. En `Stream Url`, escribe `http://IP_DEL_TELEFONO:8080/vuforia.mjpg`.
4. Deja `Stream Mode` en `Auto` o usa `Mjpeg`.
5. Ajusta `Screen Size Meters`, `Screen Local Position` y `Screen Local Euler Angles` para ubicar la pantalla donde la necesitas.
6. Ejecuta la escena. La superficie generada por `ExternalCameraScreen` mostrara el video en tiempo real.

## Modo snapshot

Si prefieres recibir imagenes sueltas, usa:

- En el telefono: `http://IP_DEL_TELEFONO:8080/snapshot.jpg`
- En el simulador: `Stream Mode = Snapshot Polling`

MJPEG es mejor para video continuo; snapshot polling es util para depurar o redes inestables.
