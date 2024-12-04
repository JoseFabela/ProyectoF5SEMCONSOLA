using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ProyectoF5SEMCONSOLA
{
    internal class Program
    {
        static string connectionString = "Server=localhost;Database=ProyectoF5Sem;Integrated Security=True;";

        static void Main(string[] args)
        {
            while (true) {
                // Menú de opciones de inicio
                Console.WriteLine("Seleccione una opción:");
                Console.WriteLine("1. Iniciar sesión");
                Console.WriteLine("2. Salir");

                string opcion = Console.ReadLine();

                if (opcion == "1")
                {
                    // Iniciar sesión
                    Console.WriteLine("Ingrese su nombre de usuario:");
                    string usuario = Console.ReadLine();

                    Console.WriteLine("Ingrese su contraseña:");
                    string contrasena = Console.ReadLine();

                    // Obtenemos el rol del usuario al autenticarlo
                    int rolId;
                    string estado;
                    if (AutenticarUsuario(usuario, contrasena, out rolId, out estado))
                    {
                        if (estado == "activo")
                        {
                            Console.WriteLine("Inicio de sesión exitoso.");

                            // Verificamos el rol del usuario
                            if (rolId == 2) // Rol administrativo
                            {
                                // Mostrar el menú de CRUD si el rol es administrativo
                                MostrarMenuCrud(usuario);
                            }
                            else if (rolId == 1) // Rol empleado
                            {
                                // Mostrar el menú de empleado si el rol es empleado
                                MostrarMenuEmpleado();
                            }
                            else
                            {
                                Console.WriteLine("Rol no reconocido. Acceso denegado.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Este usuario no esta activo");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Credenciales incorrectas. Acceso denegado.");
                    }
                }
                else if (opcion == "2")
                {
                    break;
                }
                
                else
                {
                    Console.WriteLine("Opción no válida.");
                }
            }
        }
        static bool AutenticarUsuario(string nombreUsuario, string contrasena, out int rolId, out string estado)
        {
            // Inicializamos rolId para asegurar que siempre tenga un valor
            rolId = 0;
            estado = string.Empty;
            // Conexión a la base de datos

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    // Abrir conexión
                    connection.Open();

                    // Consulta SQL para verificar si el usuario existe y obtener su rol
                    string query = "SELECT rol_id, estado FROM empleado WHERE usuario = @usuario AND contrasena = @contrasena";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Parámetros para evitar inyección SQL
                        command.Parameters.AddWithValue("@usuario", nombreUsuario);
                        command.Parameters.AddWithValue("@contrasena", EncriptarContrasena(contrasena)); // Asegúrate de encriptar la contraseña

                        // Ejecutar la consulta y obtener el rol_id del usuario
                        object result = command.ExecuteScalar();

                        // Si result es null, significa que las credenciales son incorrectas
                        if (result != null)
                        {
                            // Convertimos el valor a int
                            rolId = Convert.ToInt32(result);
                            estado= Convert.ToString(result);
                            return true; // El usuario y la contraseña son correctos
                        }
                        else
                        {
                            return false; // No se encontró el usuario o la contraseña no es correcta
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    return false;
                }
            }
        }
        static void CrearEmpleado(string usuario)
        {
            // Solicitar datos del nuevo empleado
            Console.WriteLine("Ingrese el nombre del empleado:");
            string nombre = Console.ReadLine();

            Console.WriteLine("Ingrese el ID del puesto de trabajo (1.Recepcionista, 2.Mesero, 3.Seguridad, 4.Bartender, 5.Gerente, 6.Chef):");
            int puestoId = int.Parse(Console.ReadLine());

            Console.WriteLine("Ingrese el nombre de usuario:");
            string usuarioempleado = Console.ReadLine();

            Console.WriteLine("Ingrese la contraseña:");
            string contrasena = Console.ReadLine();

            // Mostrar los roles disponibles
            Console.WriteLine("Seleccione el rol para este empleado:");
            Console.WriteLine("1. Empleado");
            Console.WriteLine("2. Administrativo");
            int rolId = int.Parse(Console.ReadLine()); // El usuario selecciona 1 o 2

            // Conexión a la base de datos
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    // Abrir conexión
                    connection.Open();

                    // Ahora se inserta el nuevo empleado en la base de datos
                    string query = "INSERT INTO empleado (nombre, puesto_trabajo_id, fecha_ingreso, estado, usuario, contrasena, rol_id) " +
                                   "VALUES (@nombre, @puestoId, GETDATE(), 'activo', @usuario, @contrasena, @rolId)";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@nombre", nombre);
                        command.Parameters.AddWithValue("@puestoId", puestoId);
                        command.Parameters.AddWithValue("@usuario", usuarioempleado);
                        command.Parameters.AddWithValue("@contrasena", EncriptarContrasena(contrasena)); // Asegúrate de encriptar la contraseña
                        command.Parameters.AddWithValue("@rolId", rolId);

                        // Ejecutar la consulta
                        command.ExecuteNonQuery();
                        Console.WriteLine("Empleado creado con éxito.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
            // Después de insertar un empleado, registramos la operación en la tabla de auditoría
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "INSERT INTO AuditLog (usuario, tabla_modificada, operacion, datos_nuevo) " +
                               "VALUES (@usuario, 'empleado', 'INSERT', @datosNuevo)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@usuario", usuario);
                    command.Parameters.AddWithValue("@datosNuevo", "Nombre: " + nombre + ", Puesto: " + puestoId);

                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }

        }

        static void MostrarMenuCrud(string usuario)
        {
            bool salir = true;
            while (salir)
            {
                Console.WriteLine("\nMenú de CRUD para Empleados:");
                Console.WriteLine("1. Ver empleados");
                Console.WriteLine("2. Crear empleado");
                Console.WriteLine("3. Actualizar empleado");
                Console.WriteLine("4. Ver Auditoria");
                Console.WriteLine("5. Salir");

                string opcionCrud = Console.ReadLine();

                switch (opcionCrud)
                {
                    case "1":
                        ObtenerEmpleados();
                        break;
                    case "2":
                        CrearEmpleado(usuario);
                        break;
                    case "3":
                        Console.WriteLine("Ingrese el ID del empleado a actualizar:");
                        int empleadoIdActualizar = int.Parse(Console.ReadLine());
                        ActualizarEmpleado(empleadoIdActualizar, usuario); // Pasar el nombre del usuario
                        break;
                    case "4":
                        VerAuditoria();
                        break;
                    case "5":
                        Console.WriteLine("Saliendo del menú de CRUD.");
                        salir = false;
                        break;
                    default:
                        Console.WriteLine("Opción no válida.");
                        break;
                }
            }
        }

        static void ObtenerEmpleados()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT id, nombre, puesto_trabajo_id, estado, usuario FROM empleado";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            Console.WriteLine("ID: {0}, Nombre: {1}, Usuario: {2}, Estado: {3}",
                                reader["id"], reader["nombre"], reader["usuario"], reader["estado"]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
        }
        static void VerAuditoria()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT * FROM AuditLog ORDER BY fecha DESC";  // Mostrar registros más recientes primero

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        Console.WriteLine($"ID: {reader["id"]}, Usuario: {reader["usuario"]}, Tabla: {reader["tabla_modificada"]}, " +
                                          $"Operación: {reader["operacion"]}, Fecha: {reader["fecha"]}");
                    }
                }
            }
        }


        static void ActualizarEmpleado(int empleadoId, string usuario)
        {
            Console.WriteLine("\nActualizar Datos de Empleado:");

            // Solicitar el nuevo estado y puesto de trabajo
            Console.Write("Ingrese el nuevo estado (activo/inactivo): ");
            string nuevoEstado = Console.ReadLine();

            // Asegurarse de que el estado sea válido
            while (nuevoEstado != "activo" && nuevoEstado != "inactivo")
            {
                Console.Write("Estado inválido. Ingrese el nuevo estado (ACTIVO/INACTIVO): ");
                nuevoEstado = Console.ReadLine().ToUpper();
            }

            Console.WriteLine("Ingrese el nuevo puesto de trabajo:");
            Console.WriteLine("1. Recepcionista");
            Console.WriteLine("2. Mesero");
            Console.WriteLine("3. Seguridad");
            Console.WriteLine("4. Bartender");
            Console.WriteLine("5. Gerente");
            Console.WriteLine("6. Chef");
            int nuevoPuestoId = int.Parse(Console.ReadLine());  // Leer el nuevo puesto

            // Crear la conexión a la base de datos
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Consulta SQL para obtener los datos actuales del empleado (antes de la actualización)
                string selectQuery = "SELECT estado, puesto_trabajo_id FROM empleado WHERE id = @empleadoId";
                using (SqlCommand selectCmd = new SqlCommand(selectQuery, conn))
                {
                    selectCmd.Parameters.AddWithValue("@empleadoId", empleadoId);

                    SqlDataReader reader = selectCmd.ExecuteReader();
                    string estadoAnterior = "", puestoAnterior = "";
                    if (reader.Read())
                    {
                        estadoAnterior = reader["estado"].ToString();
                        puestoAnterior = reader["puesto_trabajo_id"].ToString();
                    }
                    reader.Close();  // Cerrar el reader antes de seguir

                    // Actualizar el empleado en la base de datos
                    string updateQuery = "UPDATE empleado SET estado = @nuevoEstado, puesto_trabajo_id = @nuevoPuestoId " +
                                         "WHERE id = @empleadoId";  // Usar 'id' como columna para el identificador

                    using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@nuevoEstado", nuevoEstado);
                        updateCmd.Parameters.AddWithValue("@nuevoPuestoId", nuevoPuestoId);
                        updateCmd.Parameters.AddWithValue("@empleadoId", empleadoId);

                        try
                        {
                            // Ejecutar la consulta de actualización
                            int rowsAffected = updateCmd.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                Console.WriteLine("Empleado actualizado con éxito.");

                                // Registrar la auditoría en la tabla de log
                                string auditQuery = "INSERT INTO AuditLog (usuario, tabla_modificada, operacion, datos_anterior, datos_nuevo, fecha) " +
                                                    "VALUES (@usuario, 'empleado', 'UPDATE', @datosAnterior, @datosNuevo, GETDATE())";

                                using (SqlCommand auditCmd = new SqlCommand(auditQuery, conn))
                                {
                                    auditCmd.Parameters.AddWithValue("@usuario", usuario);
                                    auditCmd.Parameters.AddWithValue("@datosAnterior", $"Estado: {estadoAnterior}, Puesto: {puestoAnterior}");
                                    auditCmd.Parameters.AddWithValue("@datosNuevo", $"Estado: {nuevoEstado}, Puesto: {nuevoPuestoId}");

                                    auditCmd.ExecuteNonQuery();  // Registrar el cambio en la auditoría
                                }

                                Console.WriteLine("Cambio registrado en la auditoría.");
                            }
                            else
                            {
                                Console.WriteLine("No se encontró el empleado con el ID proporcionado.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error: " + ex.Message);
                        }
                    }
                }
            }
        }



        static void MostrarMenuEmpleado()
        {
            Console.WriteLine("\nMenú de Empleado:");
            Console.WriteLine("1. Ver menu de Jugadores");
            Console.WriteLine("2. Actualizar mi perfil");
            Console.WriteLine("3. Salir");

            string opcionEmpleado = Console.ReadLine();

            switch (opcionEmpleado)
            {
                case "1":
                    MenuDeJugador();
                    break;
                default:
                    Console.WriteLine("Opción no válida.");
                    break;
            }
        }

        static string EncriptarContrasena(string contrasena)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // Convertir la contraseña a bytes
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(contrasena));

                // Convertir los bytes a una cadena hexadecimal
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        static void MenuDeJugador()
        {
            bool salir= true;
            while (salir)
            {
                Console.WriteLine("\nMenú de Jugadores:");
                Console.WriteLine("1. Registrar Jugador");
                Console.WriteLine("2. Consultar Jugador");
                Console.WriteLine("3. Modificar Jugador");
                Console.WriteLine("4. Cambiar Estado de Jugador");
                Console.WriteLine("5. Salir");

                string opcionJugador = Console.ReadLine();


                switch (opcionJugador)
                {
                    case "1":
                        RegistrarJugador();
                        break;
                    case "2":
                        ConsultarJugador();
                        break;
                    case "3":
                        ModificarJugador();
                        break;
                    case "4":
                        CambiarEstadoJugador();
                        break;
                        case "5":
                        salir = false;
                        break;
                    default:
                        Console.WriteLine("Opción no válida.");
                        break;
                }
            }
        }





        // Método para registrar un jugador
        static void RegistrarJugador()
        {
            Console.WriteLine("\nRegistrar Nuevo Jugador:");
            Console.Write("Ingrese nombre: ");
            string nombre = Console.ReadLine();
            Console.Write("Ingrese email: ");
            string email = Console.ReadLine();
            Console.Write("Ingrese saldo: ");
            decimal saldo = decimal.Parse(Console.ReadLine());

            // Suponiendo que el estado es "ACTIVO" por defecto al registrar
            string estado = "ACTIVO";

            // Crear la conexión a la base de datos
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Consulta SQL para insertar el nuevo jugador
                string query = "INSERT INTO jugador (nombre, email, saldo, estado, fecha_registro) " +
                               "VALUES (@nombre, @email, @saldo, @estado, GETDATE()); " +
                               "SELECT SCOPE_IDENTITY();";  // Esto obtiene el ID del nuevo registro insertado

                // Crear el comando SQL con parámetros para evitar inyecciones SQL
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@nombre", nombre);
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@saldo", saldo);
                    cmd.Parameters.AddWithValue("@estado", estado);

                    // Ejecutar la consulta y obtener el ID generado
                    var result = cmd.ExecuteScalar();  // Ejecuta la consulta y devuelve el valor de SCOPE_IDENTITY()

                    // Verificar si el resultado es un número válido
                    if (result != null && int.TryParse(result.ToString(), out int idJugador))
                    {
                        Console.WriteLine($"Jugador registrado con éxito! ID asignado: {idJugador}");
                    }
                    else
                    {
                        Console.WriteLine("Error al obtener el ID del jugador.");
                    }
                }
            }

            // Mostrar los detalles del jugador registrado
            Console.WriteLine($"Nombre: {nombre}, Email: {email}, Saldo: {saldo}, Estado: {estado}");
            Console.WriteLine("\nPresione una tecla para continuar...");
            Console.ReadKey();
        }



        // Método para consultar un jugador
        static void ConsultarJugador()
        {
            Console.WriteLine("\nConsultar Jugador:");
            Console.Write("Ingrese el ID del jugador: ");
            int idJugador = int.Parse(Console.ReadLine());

            // Crear la conexión a la base de datos
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Consulta SQL para obtener los detalles del jugador
                string query = "SELECT nombre, email, saldo, estado FROM jugador WHERE id = @idJugador";

                // Crear el comando SQL con parámetros
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@idJugador", idJugador);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string nombre = reader["nombre"].ToString();
                            string email = reader["email"].ToString();
                            decimal saldo = Convert.ToDecimal(reader["saldo"]);
                            string estado = reader["estado"].ToString();

                            Console.WriteLine($"Detalles del jugador {idJugador}:");
                            Console.WriteLine($"Nombre: {nombre}");
                            Console.WriteLine($"Email: {email}");
                            Console.WriteLine($"Saldo: {saldo}");
                            Console.WriteLine($"Estado: {estado}");
                        }
                        else
                        {
                            Console.WriteLine("Jugador no encontrado.");
                        }
                    }
                }
            }

            Console.WriteLine("\nPresione una tecla para continuar...");
            Console.ReadKey();
        }


        // Método para modificar un jugador
        static void ModificarJugador()
        {
            Console.WriteLine("\nModificar Jugador:");
            Console.Write("Ingrese el ID del jugador que desea modificar: ");
            int idJugador = int.Parse(Console.ReadLine());

            // Crear la conexión a la base de datos
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Consulta SQL para obtener los detalles actuales del jugador
                string query = "SELECT saldo FROM jugador WHERE id = @idJugador";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@idJugador", idJugador);
                    decimal saldoActual = 0;

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            saldoActual = Convert.ToDecimal(reader["saldo"]);
                            Console.WriteLine($"Saldo actual: {saldoActual}");
                        }
                        else
                        {
                            Console.WriteLine("Jugador no encontrado.");
                            return;
                        }
                    }

                    // Pedir el nuevo saldo
                    Console.Write("Ingrese nuevo saldo: ");
                    decimal saldoNuevo = decimal.Parse(Console.ReadLine());

                    // Consulta SQL para actualizar el saldo
                    string updateQuery = "UPDATE jugador SET saldo = @saldoNuevo WHERE id = @idJugador";

                    using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@saldoNuevo", saldoNuevo);
                        updateCmd.Parameters.AddWithValue("@idJugador", idJugador);

                        updateCmd.ExecuteNonQuery();  // Ejecutar la actualización
                    }

                    Console.WriteLine("Jugador modificado con éxito!");
                    Console.WriteLine($"Nuevo saldo: {saldoNuevo}");
                }
            }

            Console.WriteLine("\nPresione una tecla para continuar...");
            Console.ReadKey();
        }


        // Método para cambiar el estado de un jugador
        static void CambiarEstadoJugador()
        {
            Console.WriteLine("\nCambiar Estado de Jugador:");
            Console.Write("Ingrese el ID del jugador: ");
            int idJugador = int.Parse(Console.ReadLine());

            // Crear la conexión a la base de datos
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Consulta SQL para obtener el estado actual del jugador
                string query = "SELECT estado FROM jugador WHERE id = @idJugador";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@idJugador", idJugador);
                    string estadoActual = string.Empty;

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            estadoActual = reader["estado"].ToString();
                            Console.WriteLine($"Estado actual: {estadoActual}");
                        }
                        else
                        {
                            Console.WriteLine("Jugador no encontrado.");
                            return;
                        }
                    }

                    // Cambiar estado
                    string nuevoEstado;
                    do
                    {
                        Console.Write("Ingrese el nuevo estado (ACTIVO/INACTIVO): ");
                        nuevoEstado = Console.ReadLine().ToUpper();
                    } while (nuevoEstado != "ACTIVO" && nuevoEstado != "INACTIVO");

                    // Consulta SQL para actualizar el estado
                    string updateQuery = "UPDATE jugador SET estado = @nuevoEstado WHERE id = @idJugador";

                    using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@nuevoEstado", nuevoEstado);
                        updateCmd.Parameters.AddWithValue("@idJugador", idJugador);

                        updateCmd.ExecuteNonQuery();  // Ejecutar la actualización
                    }

                    Console.WriteLine($"Estado de jugador {idJugador} cambiado a {nuevoEstado}.");
                }
            }

            Console.WriteLine("\nPresione una tecla para continuar...");
            Console.ReadKey();
        }









    }
}
