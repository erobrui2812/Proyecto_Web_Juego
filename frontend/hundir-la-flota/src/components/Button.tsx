const Button: React.FC<ButtonProps> = ({
  children,
  label,
  onClick = () => {},
  type = "button",
  className = "",
  loading = false,
}) => {
  return (
    <button
      onClick={onClick}
      type={type}
      className={`${className} ${loading ? "opacity-50 cursor-not-allowed" : ""}`}
      disabled={loading}
    >
      {loading ? "Cargando..." : label || children}
    </button>
  );
};

export default Button;
