# Compras de Mil Gemas

La tienda de monedas y potenciadores que usa **monedas del juego** funciona sin cuentas externas. Unity IAP 5.4.4 está integrado para Android e iOS, pero los botones de dinero real siguen inactivos hasta disponer de Google Play Console, App Store Connect y un servicio HTTPS que verifique recibos. No se entregan productos por tocar un botón ni por un recibo sin verificar.

## IDs para registrar en ambas tiendas

| ID | Tipo | Entrega después de la verificación |
| --- | --- | --- |
| `milgemas.coins.small` | Consumible | 1.000 monedas |
| `milgemas.coins.medium` | Consumible | 5.500 monedas |
| `milgemas.coins.large` | Consumible | 12.000 monedas |
| `milgemas.pass.monthly` | Consumible | Pase premium del mes actual |

El precio lo determina cada tienda y región; la interfaz muestra el precio localizado que devuelve Unity IAP. El pase se compra por temporada mensual, no como suscripción automática.

## Activación futura

1. Registrar los cuatro IDs en Google Play Console y App Store Connect y probarlos en entornos de prueba de ambas tiendas.
2. Implementar un servicio HTTPS que valide el recibo contra Google/Apple, compruebe producto y transaction ID, deduplique transacciones y devuelva `{"valid":true,"transactionId":"...","productId":"..."}` solo si corresponde entregar ese producto. La petición que envía el cliente incluye `transactionId`, `productId`, `receipt` y `platform`.
3. Configurar la URL HTTPS en [StoreSettings.asset](Assets/Resources/StoreSettings.asset) y activar `EnableRealMoneyPurchases`. No poner claves ni secretos de las tiendas en el cliente.
4. Probar compra, cancelación, reintento, restauración, cierre entre entrega y confirmación, y cuenta en otro dispositivo. Vincular el perfil a una identidad/backend antes de aceptar compras reales: PlayerPrefs solo conserva el perfil local.

El puente IAP deja la orden pendiente si falla la verificación. Solo después de guardar la entrega una vez por transaction ID confirma la compra a la tienda.
