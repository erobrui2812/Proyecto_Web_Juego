import PropTypes from 'prop-types';

interface ButtonProps {
  label: string;
  onClick?: () => void;
  type?: 'button' | 'submit' | 'reset';
  styleType?: string;
  className?: string;
}

const Button: React.FC<ButtonProps> = ({
  label,
  onClick = () => {},
  type = "button",
  styleType = "btnDefault",
  className = "",
}) => {
  return (
    <button
      onClick={onClick}
      type={type}
      className={className}
    >
      {label}
    </button>
  );
};

Button.propTypes = {
  label: PropTypes.string.isRequired,
  onClick: PropTypes.func,
  type: PropTypes.string,
  styleType: PropTypes.string,
  className: PropTypes.string,
};

export default Button;
